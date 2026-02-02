namespace Tracker.Dotnet.Libs.KafkaConsumer.Inbox.Configuration;

public class TransactionalInboxOptions
{
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(2);
}
