namespace Tracker.Dotnet.Libs.KafkaProducer;
public class KafkaProducerOptions
{
    public string BootstrapServers { get; set; } = string.Empty;

    public KafkaAcks Acks { get; set; } = KafkaAcks.None;

    public bool Idempotency { get; set; }

    public Dictionary<Type, string> MessageTopicMap { get; } = new();
}

public enum KafkaAcks
{
    None,
    Leader,
    All
}
