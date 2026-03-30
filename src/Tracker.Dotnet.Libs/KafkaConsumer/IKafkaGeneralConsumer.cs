namespace Tracker.Dotnet.Libs.KafkaConsumer;

/// <summary>
/// Runs message consuming on start the application.
/// </summary>
public interface IKafkaGeneralConsumer : IDisposable
{
    public Task StartConsumeAsync(CancellationToken cancellationToken);
}
