namespace Tracker.Dotnet.Libs.KafkaConsumer.Inbox.Abstractions;

public interface IInbox
{
    public Task<InboxAcquireResult> TryAcquireAsync(
        string messageId,
        CancellationToken cancellationToken = default);

    public Task MarkProcessedAsync(string messageId, CancellationToken cancellationToken = default);

    public Task MarkFailedAsync(string messageId, Exception ex, CancellationToken cancellationToken = default);
}

public enum InboxAcquireResult
{
    Acquired,
    AlreadyProcessed,
    Locked
}