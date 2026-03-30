using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tracker.Dotnet.Libs.KafkaConsumer.Inbox.Abstractions;
using Tracker.Dotnet.Libs.KafkaConsumer.Inbox.Configuration;
using Tracker.Dotnet.Libs.KafkaConsumer.Inbox.Entities;

namespace Tracker.Dotnet.Libs.KafkaConsumer.Inbox.EFCore;

public class EfCoreInbox<TDbContext> : IInbox
    where TDbContext : InboxDbContextBase
{
    private readonly ILogger<EfCoreInbox<TDbContext>> _logger;
    private readonly TDbContext _dbContext;
    private readonly TransactionalInboxOptions _transactionalInboxOptions;

    public EfCoreInbox(ILogger<EfCoreInbox<TDbContext>> logger, TDbContext dbContext, TransactionalInboxOptions transactionalInboxOptions)
    {
        _logger = logger;
        _dbContext = dbContext;
        _transactionalInboxOptions = transactionalInboxOptions;
    }

    public async Task<InboxAcquireResult> TryAcquireAsync(string messageId, Guid ownerId, CancellationToken cancellationToken)
    {
        var message = await _dbContext.InboxMessages.SingleOrDefaultAsync(m => m.MessageId == messageId, cancellationToken);

        if (message is null)
        {
            _dbContext.InboxMessages.Add(new InboxMessage
            {
                MessageId = messageId,
                OwnerId = ownerId,
                Attempts = 0,
                FirstSeenAt = DateTime.UtcNow,
                LockUntil = DateTime.UtcNow + _transactionalInboxOptions.LockInterval,
                Status = InboxStatus.Processing,
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
            return InboxAcquireResult.Acquired;
        }

        if (message.Status == InboxStatus.Processed || message.Status == InboxStatus.Failed)
        {
            return InboxAcquireResult.AlreadyProcessed;
        }

        if (message.OwnerId == ownerId && message.Status == InboxStatus.Processing)
        {
            message.Attempts++;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return InboxAcquireResult.Acquired;
        }

        if (message.OwnerId != ownerId && message.Status == InboxStatus.Processing)
        {
            if (message.LockUntil >= DateTimeOffset.UtcNow)
            {
                return InboxAcquireResult.Locked;
            }

            message.OwnerId = ownerId;
            message.LockUntil = DateTimeOffset.UtcNow + _transactionalInboxOptions.LockInterval;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return InboxAcquireResult.Acquired;
        }

        throw new InvalidOperationException("Unknown case for acquiring inbox message");
    }

    public async Task MarkFailedAsync(string messageId, Exception ex, CancellationToken cancellationToken)
    {
        var message = await _dbContext.InboxMessages.SingleOrDefaultAsync(m => m.MessageId == messageId, cancellationToken);

        if (message is null)
        {
            _logger.LogError("Message Id {MessageId} was not found in Inbox db to mark as failed", messageId);
            return;
        }

        message.Status = InboxStatus.Failed;
        message.ProcessedAt = DateTimeOffset.UtcNow;
        message.LastError = ex.Message;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkProcessedAsync(string messageId, CancellationToken cancellationToken)
    {
        var message = await _dbContext.InboxMessages.SingleOrDefaultAsync(m => m.MessageId == messageId, cancellationToken);

        if (message is null)
        {
            _logger.LogError("Message Id {MessageId} was not found in Inbox db to mark as processed", messageId);
            return;
        }

        message.Status = InboxStatus.Processed;
        message.ProcessedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
