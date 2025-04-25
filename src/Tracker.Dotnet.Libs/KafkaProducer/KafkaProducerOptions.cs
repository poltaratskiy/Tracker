namespace Tracker.Dotnet.Libs.KafkaProducer;
public class KafkaProducerOptions
{
    public string BootstrapServers { get; set; } = string.Empty;

    public Dictionary<Type, string> MessageTopicMap { get; } = new();
}
