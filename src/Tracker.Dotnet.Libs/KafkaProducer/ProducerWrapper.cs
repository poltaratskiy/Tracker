using Confluent.Kafka;

namespace Tracker.Dotnet.Libs.KafkaProducer
{
    public class ProducerWrapper : IProducerWrapper
    {
        private readonly IProducer<string, string> _producer;

        public ProducerWrapper(KafkaProducerOptions options)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = options.BootstrapServers
            };

            _producer = new ProducerBuilder<string, string>(config).Build();
        }

        public Task<DeliveryResult<string, string>> ProduceAsync(string topic, Message<string, string> message, CancellationToken cancellationToken)
        {
            return _producer.ProduceAsync(topic, message, cancellationToken);
        }
    }
}
