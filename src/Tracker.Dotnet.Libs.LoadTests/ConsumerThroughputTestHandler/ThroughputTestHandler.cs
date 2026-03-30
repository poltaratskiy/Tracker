using Tracker.Dotnet.Libs.KafkaAbstractions;
using Tracker.Dotnet.Libs.KafkaConsumer;
using Tracker.Dotnet.Libs.LoadTests.Infrastructure;
using Tracker.Dotnet.Libs.LoadTests.Persistence;
using Tracker.Dotnet.Libs.LoadTests.Persistence.Entities;

namespace Tracker.Dotnet.Libs.LoadTests.ConsumerThroughputTestHandler;

public class ThroughputTestHandler : IHandler<ThroughputTestMessage>
{
    private readonly ProcessingCompletitionTracker _processingCompletitionTracker;
    private readonly IContextAccessor _contextAccessor;
    private readonly TestDbContext _dbContext;

    public ThroughputTestHandler(
        ProcessingCompletitionTracker processingCompletitionTracker, 
        IContextAccessor contextAccessor,
        TestDbContext dbContext)
    {
        _processingCompletitionTracker = processingCompletitionTracker;
        _contextAccessor = contextAccessor;
        _dbContext = dbContext;
    }

    public async Task HandleAsync(ThroughputTestMessage message, CancellationToken cancellationToken = default)
    {
        var start = DateTime.UtcNow;

        var messageInfo = new ProcessedMessageInfo
        {
            Content = message.Content,
            DateStart = start,
            InstanceId = _contextAccessor.InstanceId,
            MessageId = _contextAccessor.MessageId,
        };

        _dbContext.ProcessedMessages.Add(messageInfo);

        var finish = DateTime.UtcNow;
        messageInfo.DateEnd = finish;
        messageInfo.Duration = finish - start;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _processingCompletitionTracker.MarkProcessed();
    }
}
