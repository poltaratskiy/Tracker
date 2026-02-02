using Tracker.Dotnet.Libs.KafkaConsumer.Inbox.Abstractions;
using static Tracker.Dotnet.Libs.KafkaConsumer.Inbox.Abstractions.IInbox;

namespace Tracker.Dotnet.Libs.KafkaConsumer.Inbox;

public class NoOpInbox : IInbox
{
    public Task<InboxAcquireResult> TryAcquireAsync(string messageId, CancellationToken cancellationToken)
        => Task.FromResult(InboxAcquireResult.Acquired);

    public Task MarkProcessedAsync(string messageId, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task MarkFailedAsync(string messageId, Exception ex, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
