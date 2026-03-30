namespace Tracker.Dotnet.Libs.KafkaConsumer;

public interface IContextAccessor
{
    /// <summary>
    /// Get consumer Id that is unique within instance of an application.
    /// </summary>
    public Guid InstanceId { get; set; }

    /// <summary>
    /// Get an unique message Id
    /// </summary>
    public string MessageId { get; set; }
}
