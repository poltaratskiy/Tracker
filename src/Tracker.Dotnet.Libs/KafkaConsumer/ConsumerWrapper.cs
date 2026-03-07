using Confluent.Kafka;

namespace Tracker.Dotnet.Libs.KafkaConsumer;

public class ConsumerWrapper : IConsumerWrapper, IDisposable
{
    private readonly KafkaConsumerOptions _consumerOptions;
    private IConsumer<Ignore, string>? _consumer;

    public ConsumerWrapper(KafkaConsumerOptions consumerOptions)
    {
        _consumerOptions = consumerOptions;
    }

    public void Subscribe()
    {
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _consumerOptions.BootstrapServers,
            GroupId = _consumerOptions.ConsumerGroup,     // consumer group name (important for balancing)
            AutoOffsetReset = AutoOffsetReset.Earliest, // read from the beginning if offset not found
            EnableAutoCommit = false,                 // control offset manually
            AllowAutoCreateTopics = false,            // specify topics
        };

        var topics = _consumerOptions.ConsumerTopicMap.Keys.ToArray();
        var builder = new ConsumerBuilder<Ignore, string>(consumerConfig);
        _consumer = builder.Build();
        _consumer.Subscribe(topics);
    }

    public ConsumeResult<Ignore, string> Consume(CancellationToken cancellationToken)
    {
        return _consumer!.Consume(cancellationToken);
    }

    public void Commit(ConsumeResult<Ignore, string> result)
    {
        _consumer!.Commit(result);
    }

    public void Dispose()
    {
        _consumer?.Close();
        _consumer?.Dispose();
    }
}
