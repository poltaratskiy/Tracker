using Tracker.Dotnet.Libs.KafkaAbstractions;
using Tracker.Dotnet.Libs.LoadTests.Infrastructure;
using Tracker.Dotnet.Libs.LoadTests.Persistence;
using Tracker.Dotnet.Libs.LoadTests.Persistence.Entities;
using Tracker.Dotnet.Libs.RequestContextAccessor.Abstractions;

namespace Tracker.Dotnet.Libs.LoadTests.ConsumerLatencyTestHandler;

public class LatencyTestHandler : IHandler<LatencyTestMessage>
{
    private readonly ProcessingCompletitionTracker _processingCompletitionTracker;
    private readonly IRequestContextAccessor _contextAccessor;
    private readonly TestDbContext _dbContext;

    public LatencyTestHandler(
        ProcessingCompletitionTracker processingCompletitionTracker,
        IRequestContextAccessor contextAccessor,
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
            MessageId = _contextAccessor.Current?.MessageId!,
            DateSent = message.DateSent,
            DateReceived = received,
            Duration = received - message.DateSent
        };

        _dbContext.LatencyTestMessages.Add(messageDb);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _processingCompletitionTracker.MarkProcessed();
    }
}
