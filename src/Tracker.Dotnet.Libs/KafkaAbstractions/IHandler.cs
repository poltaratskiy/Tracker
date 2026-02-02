namespace Tracker.Dotnet.Libs.KafkaAbstractions;

public interface IHandler<TMessage> where TMessage : IMessage
{
    public Task HandleAsync(TMessage message, CancellationToken cancellationToken = default);
}

