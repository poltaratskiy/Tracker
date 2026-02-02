using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Timeout;
using Serilog.Context;
using System.Text;
using System.Text.Json;
using Tracker.Dotnet.Libs.KafkaProducer;

namespace Tracker.Dotnet.Libs.KafkaConsumer;

public class KafkaGeneralConsumer : IKafkaGeneralConsumer
{
    private readonly ILogger<KafkaGeneralConsumer> _logger;
    private readonly IConsumerWrapper _consumerWrapper;
    private readonly IProducerWrapper _producerWrapper;
    private readonly IServiceProvider _serviceProvider;
    private readonly KafkaConsumerOptions _consumerOptions;
    private readonly Dictionary<string, MessageHandlerMap> _topicMap;

    public KafkaGeneralConsumer(
        ILogger<KafkaGeneralConsumer> logger,
        IConsumerWrapper consumerWrapper,
        IProducerWrapper producerWrapper,
        IServiceProvider serviceProvider,
        KafkaConsumerOptions consumerOptions)
    {
        _logger = logger;
        _consumerWrapper = consumerWrapper;
        _producerWrapper = producerWrapper;
        _serviceProvider = serviceProvider;
        _consumerOptions = consumerOptions;
        _topicMap = _consumerOptions.ConsumerTopicMap;
    }

    public async Task StartConsumeAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_consumerOptions.DeadLettersTopic))
        {
            _logger.LogWarning("Topic for dead letters messages is not pointed out, you won't be able to get original message in case of failure");
        }

        var policy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: _consumerOptions.InstantRetries,
                sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(attempt == 1 ? 500 : 2000),
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

                if (!_topicMap.TryGetValue(topic, out var topicHandlerMap))
                {
                    _logger.LogError($"Topic is not registered: {topic}");
                    _consumerWrapper.Commit(result);
                    continue;
                }

                var messageType = topicHandlerMap.Message;
                var handlerType = topicHandlerMap.Handler;
                var messageJson = result.Message.Value;

                _logger.LogInformation("Received message to topic {topic}, type {type}", topic, messageType.Name);

                using var scope = _serviceProvider.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService(handlerType);

                var refId = result.Message.Headers
                    .FirstOrDefault(h => h.Key == "refid")?
                    .GetValueBytes();

                var refIdStr = refId != null
                    ? Encoding.UTF8.GetString(refId)
                    : string.Empty;

                var messageId = result.Message.Headers
                    .FirstOrDefault(h => h.Key == "MessageId")?
                    .GetValueBytes();

                string? messageIdStr = null;
                if (messageId != null)
                {
                    messageIdStr = Encoding.UTF8.GetString(messageId);
                }

                using (LogContext.PushProperty("refid", refIdStr))
                {
                    try
                    {
                        var message = JsonSerializer.Deserialize(messageJson, messageType);

                        await combinedPolicy.ExecuteAsync(async ct =>
                        {
                            await ((dynamic)handler).HandleAsync((dynamic)message!, ct)!;
                            _consumerWrapper.Commit(result);
                            _logger.LogInformation("Successfully finished processing of message type {type}", messageType.Name);
                        }, cancellationToken);
                    }
                    catch (TimeoutRejectedException tex)
                    {
                        _logger.LogError(tex, "Timeout while processing message of type {type}", messageType.Name);
                        await MoveToDeadLetterTopic(messageJson, refIdStr, messageIdStr, tex.Message, cancellationToken);
                        _consumerWrapper.Commit(result);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        // Do not send to DLQ and do not commit message
                        throw;
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogError(ex, "Error occured while processing message of type {type}", messageType.Name);
                        await MoveToDeadLetterTopic(messageJson, refIdStr, messageIdStr, ex.Message, cancellationToken);
                        _consumerWrapper.Commit(result);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Consuming Kafka messages was cancelled, stopping consumer");
                break;
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Kafka consume error: {Reason}", ex.Error.Reason);

                if (ex.Error.IsFatal)
                {
                    _logger.LogCritical("Fatal Kafka error, stopping consumer");
                    break; // service stopping
                }

                // transient error - little pause
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"General error of Kafka consumer: {ex.Message}");
            }
        }
    }

    private async Task MoveToDeadLetterTopic(string serializedMessage, string? refId, string? messageId, string? reason, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_consumerOptions.DeadLettersTopic))
        {
            return;
        }

        var key = Guid.NewGuid().ToString();

        var msg = new Message<string, string>
        {
            Key = key,
            Value = serializedMessage,
        };

        msg.Headers = new Headers();

        if (refId != null)
        {
            msg.Headers.Add("refid", Encoding.UTF8.GetBytes(refId));
        }

        if (messageId != null)
        {
            msg.Headers.Add("MessageId", Encoding.UTF8.GetBytes(messageId));
        }

        await _producerWrapper.ProduceAsync(_consumerOptions.DeadLettersTopic, msg, cancellationToken);
    }
}
