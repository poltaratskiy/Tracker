namespace Tracker.Dotnet.Libs.LoadTests.Infrastructure;

public class ProcessingCompletitionTracker
{
    private readonly TaskCompletionSource _tcs = new();
    private readonly CancellationTokenSource _cts = new();
    private int _processedCount;
    private int _expectedCount;

    public void SetExpectedCount(int expectedCount)
    {
        _expectedCount = expectedCount;
    }

    public Task Completion => _tcs.Task;
    public CancellationTokenSource CTS => _cts;

    public void MarkProcessed()
    {
        var current = Interlocked.Increment(ref _processedCount);

        if (current == _expectedCount)
        {
            _tcs.TrySetResult();
        }
        else if (current > _expectedCount)
        {
            _tcs.TrySetException(new InvalidOperationException($"Processed more messages than expected. Expected: {_expectedCount}, actual: {current}"));
        }
    }
}
