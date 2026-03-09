namespace Tracker.Dotnet.Libs.LoadTests.Infrastructure;

public class SpConfiguration
{
    public KafkaAcks KafkaAcks { get; set; }
    public bool KafkaInempotency { get; set; }
    public bool UseTransationalInbox { get; set; }
}

public enum KafkaAcks
{
    None,
    Leader,
    All
}
