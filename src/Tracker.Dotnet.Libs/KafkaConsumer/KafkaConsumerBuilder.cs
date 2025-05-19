namespace Tracker.Dotnet.Libs.KafkaConsumer;

public class KafkaConsumerBuilder
{
    private readonly KafkaConsumerOptions _options;

    public KafkaConsumerBuilder(KafkaConsumerOptions options)
    {
        _options = options;
    }

    public MessageConfigBuilder<T> ForMessage<T>()
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
}
