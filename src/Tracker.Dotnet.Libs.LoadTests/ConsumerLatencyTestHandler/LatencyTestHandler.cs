using Tracker.Dotnet.Libs.KafkaAbstractions;
using Tracker.Dotnet.Libs.KafkaConsumer;
using Tracker.Dotnet.Libs.LoadTests.Infrastructure;
using Tracker.Dotnet.Libs.LoadTests.Persistence;
using Tracker.Dotnet.Libs.LoadTests.Persistence.Entities;

namespace Tracker.Dotnet.Libs.LoadTests.ConsumerLatencyTestHandler;

public class LatencyTestHandler : IHandler<LatencyTestMessage>
{
    private readonly ProcessingCompletitionTracker _processingCompletitionTracker;
    private readonly IContextAccessor _contextAccessor;
    private readonly TestDbContext _dbContext;

    public LatencyTestHandler(
        ProcessingCompletitionTracker processingCompletitionTracker,
        IContextAccessor contextAccessor,
        TestDbContext dbContext)
    {
        _processingCompletitionTracker = processingCompletitionTracker;
        _contextAccessor = contextAccessor;
        _dbContext = dbContext;
    }

    public async Task HandleAsync(LatencyTestMessage message, CancellationToken cancellationToken = default)
    {
        var received = DateTime.UtcNow;

        var messageDb = new LatencyTestMessageInfo
        {
            Content = message.Content,
            MessageId = _contextAccessor.MessageId,
            DateSent = message.DateSent,
            DateReceived = received,
            Duration = received - message.DateSent
        };

        _dbContext.LatencyTestMessages.Add(messageDb);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _processingCompletitionTracker.MarkProcessed();
    }
}
