using Confluent.Kafka;

namespace Tracker.Dotnet.Libs.KafkaProducer;

public class ProducerWrapper : IProducerWrapper
{
    private readonly IProducer<string, string> _producer;

    public ProducerWrapper(KafkaProducerOptions options)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = options.BootstrapServers,
            Acks = GetAcks(options.Acks), // Acks.All is "At least once" for guarantee delivery, consumer must deduplicate
            EnableIdempotence = options.Idempotency,
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public Task<DeliveryResult<string, string>> ProduceAsync(string topic, Message<string, string> message, CancellationToken cancellationToken)
    {
        return _producer.ProduceAsync(topic, message, cancellationToken);
    }

    private static Acks GetAcks(KafkaAcks kafkaAcks) =>
        kafkaAcks switch
        {
            KafkaAcks.None => Acks.None,
            KafkaAcks.Leader => Acks.Leader,
            KafkaAcks.All => Acks.All,
            _ => throw new InvalidOperationException($"Invalid KafkaAcks value: {kafkaAcks}")
        };
}
