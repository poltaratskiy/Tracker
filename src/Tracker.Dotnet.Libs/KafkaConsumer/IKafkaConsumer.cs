namespace Tracker.Dotnet.Libs.KafkaConsumer;

/// <summary>
/// Process kafka message.
/// </summary>
public interface IKafkaConsumer<T> where T : class
{
    public Task HandleAsync(T message, CancellationToken cancellationToken = default);
}
