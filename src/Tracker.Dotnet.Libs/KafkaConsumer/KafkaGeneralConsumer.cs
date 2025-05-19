using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Timeout;
using Serilog.Context;
using System.Text;
using System.Text.Json;

namespace Tracker.Dotnet.Libs.KafkaConsumer;

public class KafkaGeneralConsumer : IKafkaGeneralConsumer
{
    private readonly ILogger<KafkaGeneralConsumer> _logger;
    private readonly IConsumerWrapper _consumerWrapper;
    private readonly IServiceProvider _serviceProvider;
    private readonly KafkaConsumerOptions _consumerOptions;
    private readonly Dictionary<string, (Type, Type)> _topicMap;

    // It is overhead but I want to demonstrate how it is possible to limit the load.
    private readonly SemaphoreSlim _concurrencyLimiter = new(16); // maximum 16 messages are processed simultaneously. 

    public KafkaGeneralConsumer(
        ILogger<KafkaGeneralConsumer> logger,
        IConsumerWrapper consumerWrapper,
        IServiceProvider serviceProvider,
        KafkaConsumerOptions consumerOptions)
    {
        _logger = logger;
        _consumerWrapper = consumerWrapper;
        _serviceProvider = serviceProvider;
        _consumerOptions = consumerOptions;
        _topicMap = _consumerOptions.ConsumerTopicMap;
    }

    public async Task StartConsumeAsync(CancellationToken cancellationToken)
    {
        var policy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (exception, delay, attempt, ctx) =>
                {
                    _logger.LogWarning(exception,
                        "Retry {Attempt} after {Delay}s while processing message",
                               attempt, delay.TotalSeconds);
                });

        var timeoutPolicy = Policy
            .TimeoutAsync(TimeSpan.FromSeconds(30), Polly.Timeout.TimeoutStrategy.Pessimistic);

        var combinedPolicy = Policy.WrapAsync(policy, timeoutPolicy);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = _consumerWrapper.Consume(cancellationToken);
                var topic = result.Topic;

                if (!_topicMap.TryGetValue(topic, out var tuple))
                {
                    _logger.LogError($"Topic is not registered: {topic}");
                    continue;
                }

                var (messageType, handlerType) = tuple;
                var messageJson = result.Message.Value;
                var message = JsonSerializer.Deserialize(messageJson, messageType);

                _logger.LogInformation("Received message to topic {topic}, type {type}", topic, messageType.Name);

                using var scope = _serviceProvider.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService(handlerType);

                var method = handlerType.GetMethod("HandleAsync");
                if (method == null)
                {
                    _logger.LogError($"Method HandleAsync not found at {handlerType.Name}");
                    continue;
                }

                var correlationId = result.Message.Headers
                    .FirstOrDefault(h => h.Key == "refid")?
                    .GetValueBytes();

                await _concurrencyLimiter.WaitAsync(cancellationToken);

                // Asynchronous processing in background
                _ = Task.Run(async () =>
                {
                    var runTopic = topic;
                    var messageTypeName = messageType.Name;
                    var localHandler = handler;
                    var localMessage = message;
                    var localMethod = method;

                    var correlationIdStr = correlationId != null
                    ? Encoding.UTF8.GetString(correlationId)
                    : string.Empty;

                    // Assign end-to-end identifier
                    using (LogContext.PushProperty("refid", correlationIdStr))
                    {
                        try
                        {
                            await combinedPolicy.ExecuteAsync(async ct =>
                            {
                                var task = (Task)localMethod.Invoke(localHandler, new[] { localMessage!, cancellationToken })!;
                                await task; // task contains handler calling
                                _consumerWrapper.Commit(result);
                                _logger.LogInformation("Successfully finished processing of message type {type}", messageTypeName);
                            }, cancellationToken);
                        }
                        catch (TimeoutRejectedException tex)
                        {
                            _logger.LogError(tex, "Timeout while processing message of type {type}", messageTypeName);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Error occured while processing message from Kafka: {ex.Message}");
                        }
                        finally
                        {
                            _concurrencyLimiter.Release();
                        }
                    }
                }, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Consuming Kafka messages was cancelled, stopping consumer");
                break;
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, $"Kafka consume error: {ex.Error.Reason}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"General error of Kafka consumer: {ex.Message}");
            }
        }
    }
}
