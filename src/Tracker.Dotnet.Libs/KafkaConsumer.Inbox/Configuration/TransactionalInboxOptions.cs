namespace Tracker.Dotnet.Libs.KafkaConsumer.Inbox.Configuration;

public class TransactionalInboxOptions
{
    public TimeSpan LockInterval { get; set; } = TimeSpan.FromSeconds(20);
}
