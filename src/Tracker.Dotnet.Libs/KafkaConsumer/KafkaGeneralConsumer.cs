using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Timeout;
using Serilog.Context;
using System.Text;
using System.Text.Json;
using Tracker.Dotnet.Libs.KafkaConsumer.Inbox.Abstractions;
using Tracker.Dotnet.Libs.KafkaConsumer.Inbox.Configuration;
using Tracker.Dotnet.Libs.KafkaProducer;
using Tracker.Dotnet.Libs.RequestContextAccessor.Abstractions;

namespace Tracker.Dotnet.Libs.KafkaConsumer;

public class KafkaGeneralConsumer : IKafkaGeneralConsumer
{
    // Instance Id is unique Id if there are a few instance deployed simultaneously
    // and transactional inbox could allow to retry processing if MessageId has status "Locked" but disallow for other instances.
    private readonly Guid InstanceId = Guid.NewGuid();

    private readonly ILogger<KafkaGeneralConsumer> _logger;
    private readonly IConsumerWrapper _consumerWrapper;
    private readonly IProducerWrapper _producerWrapper;
    private readonly IRequestContextAccessor _requestContextAccessor;
    private readonly IServiceProvider _serviceProvider;
    private readonly IInbox _inbox;
    private readonly KafkaConsumerOptions _consumerOptions;
    private readonly TransactionalInboxOptions _transactionalInboxOptions;
    private readonly Dictionary<string, MessageHandlerMap> _topicMap;

    public KafkaGeneralConsumer(
        ILogger<KafkaGeneralConsumer> logger,
        IConsumerWrapper consumerWrapper,
        IProducerWrapper producerWrapper,
        IRequestContextAccessor requestContextAccessor,
        IServiceProvider serviceProvider,
        IInbox inbox,
        TransactionalInboxOptions transactionalInboxOptions,
        KafkaConsumerOptions consumerOptions)
    {
        _logger = logger;
        _consumerWrapper = consumerWrapper;
        _producerWrapper = producerWrapper;
        _requestContextAccessor = requestContextAccessor;
        _serviceProvider = serviceProvider;
        _consumerOptions = consumerOptions;
        _inbox = inbox;
        _transactionalInboxOptions = transactionalInboxOptions;
        _topicMap = _consumerOptions.ConsumerTopicMap;
    }

    public Task StartConsumeAsync(CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
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
                .TimeoutAsync(TimeSpan.FromSeconds(30), TimeoutStrategy.Pessimistic);

            var combinedPolicy = Policy.WrapAsync(policy, timeoutPolicy);

            _consumerWrapper.Subscribe();

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

                    var messageId = result.Message.Headers
                        .First(h => h.Key == "MessageId")
                        .GetValueBytes();

                    var messageIdStr = Encoding.UTF8.GetString(messageId);
                    _logger.LogInformation("Received message to topic {Topic}, type {Type}, message Id {MessageId}", topic, messageType.Name, messageIdStr);

                    var inboxAcquireResult = await _inbox.TryAcquireAsync(messageIdStr, InstanceId, cancellationToken);
                    if (inboxAcquireResult == InboxAcquireResult.Locked)
                    {
                        _logger.LogWarning("Received duplicated message {MessageId} while someone is processing, awaiting...", messageIdStr);
                        await Task.Delay(_transactionalInboxOptions.LockInterval);
                    }
                    else if (inboxAcquireResult == InboxAcquireResult.AlreadyProcessed)
                    {
                        _logger.LogWarning("Received duplicated message {MessageId} while it has been already processed, commiting and moving forward", messageIdStr);
                        _consumerWrapper.Commit(result);
                    }

                    using var scope = _serviceProvider.CreateScope();
                    var handler = scope.ServiceProvider.GetRequiredService(handlerType);

                    var refId = result.Message.Headers
                        .FirstOrDefault(h => h.Key == "RefId")?
                        .GetValueBytes();

                    var refIdStr = refId != null
                        ? Encoding.UTF8.GetString(refId)
                        : Guid.NewGuid().ToString("N")[^6..];

                    var token = result.Message.Headers
                        .FirstOrDefault(h => h.Key == "Authorization")?
                        .GetValueBytes();

                    var tokenStr = token != null
                        ? Encoding.UTF8.GetString(token)
                        : null;

                    var requestContext = new RequestContext
                    {
                        JwtToken = tokenStr,
                        ConsumerInstanceId = InstanceId,
                        MessageId = messageIdStr,
                        RefId = refIdStr
                    };


                    using (LogContext.PushProperty("refid", refIdStr))
                    {
                        try
                        {
                            var message = JsonSerializer.Deserialize(messageJson, messageType);

                            _requestContextAccessor.Current = requestContext;

                            await combinedPolicy.ExecuteAsync(async ct =>
                            {
                                await ((dynamic)handler).HandleAsync((dynamic)message!, ct)!;
                                await _inbox.MarkProcessedAsync(messageIdStr, cancellationToken);
                                _consumerWrapper.Commit(result);
                                _logger.LogInformation("Successfully finished processing of message type {type}", messageType.Name);
                            }, cancellationToken);
                        }
                        catch (TimeoutRejectedException tex)
                        {
                            _logger.LogError(tex, "Timeout while processing message of type {type}", messageType.Name);
                            await MoveToDeadLetterTopic(messageJson, refIdStr, messageIdStr, tex.Message, cancellationToken);
                            await _inbox.MarkFailedAsync(messageIdStr, tex, cancellationToken);
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
                            await _inbox.MarkFailedAsync(messageIdStr, ex, cancellationToken);
                            _consumerWrapper.Commit(result);
                        }
                        finally
                        {
                            _requestContextAccessor.Current = null;
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
                    await Task.Delay(_transactionalInboxOptions.LockInterval, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"General error of Kafka consumer: {ex.Message}");
                }
            }
        }, cancellationToken);
    }

    public void Dispose()
    {
        _consumerWrapper?.Dispose();
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
