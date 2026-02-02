using Tracker.Dotnet.Libs.KafkaConsumer.Inbox.Abstractions;

namespace Tracker.Dotnet.Libs.KafkaConsumer.Inbox.Internal;

public class NoOpInbox : IInbox
{
    public Task<InboxAcquireResult> TryAcquireAsync(string messageId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(InboxAcquireResult.Acquired);
    }

    public Task MarkFailedAsync(string messageId, Exception ex, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task MarkProcessedAsync(string messageId, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
