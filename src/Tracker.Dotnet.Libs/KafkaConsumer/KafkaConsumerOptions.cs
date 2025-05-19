namespace Tracker.Dotnet.Libs.KafkaConsumer;

public class KafkaConsumerOptions
{
    public string BootstrapServers { get; set; } = string.Empty;

    public string ConsumerGroup { get; set; } = string.Empty;

    /// <summary>
    /// Dictionary that contains Message type and Handler type by typic name.
    /// </summary>
    public Dictionary<string, (Type MessageType, Type HandlerType)> ConsumerTopicMap { get; } = new();
}
