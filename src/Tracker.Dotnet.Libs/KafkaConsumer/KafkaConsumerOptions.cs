namespace Tracker.Dotnet.Libs.KafkaConsumer;

public class KafkaConsumerOptions
{
    public string BootstrapServers { get; set; } = null!;

    public string ConsumerGroup { get; set; } = null!;

    public string DeadLettersTopic { get; set; } = null!;

    public int InstantRetries { get; set; }

    /// <summary>
    /// Dictionary that contains Message type and Handler type by typic name.
    /// </summary>
    public Dictionary<string, MessageHandlerMap> ConsumerTopicMap { get; } = new();
}

public record MessageHandlerMap(Type Message, Type Handler);