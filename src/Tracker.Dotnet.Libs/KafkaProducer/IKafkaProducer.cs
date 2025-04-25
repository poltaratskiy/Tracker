namespace Tracker.Dotnet.Libs.KafkaProducer
{
    /// <summary>
    /// Sends message to the defined Kafka topic
    /// </summary>
    public interface IKafkaProducer
    {
        public Task ProduceAsync(object message, CancellationToken cancellationToken = default);
    }
}
