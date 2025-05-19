using Confluent.Kafka;

namespace Tracker.Dotnet.Libs.KafkaConsumer;

/// <summary>
/// Wrapper for Kafka IConsumer to be possible to cover with unit tests
/// </summary>
public interface IConsumerWrapper
{
    public void Subscribe();

    public ConsumeResult<Ignore, string> Consume(CancellationToken cancellationToken);

    public void Commit(ConsumeResult<Ignore, string> result);
}
