using Tracker.Dotnet.Libs.KafkaAbstractions;
using Tracker.Dotnet.Libs.KafkaConsumer;
using Tracker.Dotnet.Libs.LoadTests.Infrastructure;
using Tracker.Dotnet.Libs.LoadTests.Persistence;
using Tracker.Dotnet.Libs.LoadTests.Persistence.Entities;

namespace Tracker.Dotnet.Libs.LoadTests.ConsumerTestHandler;

public class KafkaTestHandler : IHandler<KafkaTestMessage>
{
    private readonly ProcessingCompletitionTracker _processingCompletitionTracker;
    private readonly IContextAccessor _contextAccessor;
    private readonly TestDbContext _dbContext;

    public KafkaTestHandler(
        ProcessingCompletitionTracker processingCompletitionTracker, 
        IContextAccessor contextAccessor,
        TestDbContext dbContext)
    {
        _processingCompletitionTracker = processingCompletitionTracker;
        _contextAccessor = contextAccessor;
        _dbContext = dbContext;
    }

    public async Task HandleAsync(KafkaTestMessage message, CancellationToken cancellationToken = default)
    {
        // Simulate asynchronous work
        var start = DateTime.Now;

        var messageInfo = new ProcessedMessageInfo
        {
            Content = message.Content,
            DateStart = start,
            InstanceId = _contextAccessor.InstanceId,
            MessageId = _contextAccessor.MessageId,
        };

        _dbContext.ProcessedMessages.Add(messageInfo);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var finish = DateTime.Now;

        messageInfo.DateEnd = finish;
        messageInfo.Duration = start - finish;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _processingCompletitionTracker.MarkProcessed();
    }
}
