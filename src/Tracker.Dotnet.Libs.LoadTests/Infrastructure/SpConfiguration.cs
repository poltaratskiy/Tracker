using Tracker.Dotnet.Libs.KafkaProducer;

namespace Tracker.Dotnet.Libs.LoadTests.Infrastructure;

public class SpConfiguration
{
    public KafkaAcks KafkaAcks { get; set; }
    public bool KafkaIdempotency { get; set; }
    public bool UseTransationalInbox { get; set; }
}

