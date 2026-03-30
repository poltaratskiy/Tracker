namespace Tracker.Dotnet.Libs.KafkaProducer;

public class KafkaProducerBuilder
{
    private readonly KafkaProducerOptions _options;

    public KafkaProducerBuilder(KafkaProducerOptions options)
    {
        _options = options;
    }

    public MessageConfigBuilder<T> ForMessage<T>()
    {
        return new MessageConfigBuilder<T>(_options);
    }

    public KafkaProducerBuilder BootstrapServers(string servers)
    {
        _options.BootstrapServers = servers;
        return this;
    }

    public KafkaProducerBuilder Acks(KafkaAcks acks)
    {
        _options.Acks = acks;
        return this;
    }

    public KafkaProducerBuilder Idempotency(bool idempotency)
    {
        _options.Idempotency = idempotency;
        return this;
    }
}
