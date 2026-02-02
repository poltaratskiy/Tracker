using Tracker.Dotnet.Libs.KafkaAbstractions;

namespace Tracker.Dotnet.Libs.KafkaConsumer;

public class KafkaConsumerBuilder
{
    private readonly KafkaConsumerOptions _options;

    public KafkaConsumerBuilder(KafkaConsumerOptions options)
    {
        _options = options;
    }

    public MessageConfigBuilder<T> ForMessage<T>() where T : IMessage
    {
        return new MessageConfigBuilder<T>(_options);
    }

    public KafkaConsumerBuilder BootstrapServers(string servers)
    {
        _options.BootstrapServers = servers;
        return this;
    }

    public KafkaConsumerBuilder ConsumerGroup(string consumerGroup)
    {
        _options.ConsumerGroup = consumerGroup;
        return this;
    }

    public KafkaConsumerBuilder InstantRetries(int retries)
    {
        _options.InstantRetries = retries;
        return this;
    }

    public KafkaConsumerBuilder SetDeadLetterTopic(string topic)
    {
        _options.DeadLettersTopic = topic;
        return this;
    }
}
