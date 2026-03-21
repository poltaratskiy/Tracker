namespace Tracker.Dotnet.Libs.KafkaConsumer;

public sealed class ContextAccessor : IContextAccessor
{
    public Guid InstanceId { get; set; }

    public string MessageId { get; set; } = null!;
}
