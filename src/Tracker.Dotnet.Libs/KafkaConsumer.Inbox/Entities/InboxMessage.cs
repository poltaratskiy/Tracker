namespace Tracker.Dotnet.Libs.KafkaConsumer.Inbox.Entities;

public class InboxMessage
{
    public string MessageId { get; set; } = null!;
    public string Status { get; set; } = InboxStatus.Processing;

    public DateTimeOffset FirstSeenAt { get; set; }
    public DateTimeOffset LockUntil { get; set; }

    public int Attempts { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public string? LastError { get; set; }
}
