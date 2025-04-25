using Confluent.Kafka;

namespace Tracker.Dotnet.Libs.KafkaProducer
{
    /// <summary>
    /// Wrapper for Kafka IProducer to be possible to cover with unit tests
    /// </summary>
    public interface IProducerWrapper
    {
        public Task<DeliveryResult<string, string>> ProduceAsync(string topic, Message<string, string> message, CancellationToken cancellationToken);
    }
}
