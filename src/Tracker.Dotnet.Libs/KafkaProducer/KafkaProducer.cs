using Confluent.Kafka;
using System.Text.Json;

namespace Tracker.Dotnet.Libs.KafkaProducer;

public class KafkaProducer : IKafkaProducer
{
    private readonly IProducerWrapper _producer;
    private readonly KafkaProducerOptions _options;

    public KafkaProducer(KafkaProducerOptions options, IProducerWrapper producer)
    {
        _options = options;

        var config = new ProducerConfig
        {
            BootstrapServers = options.BootstrapServers
        };

        _producer = producer;
    }

    public async Task ProduceAsync(object message, CancellationToken cancellationToken = default)
    {
        var type = message.GetType();

        if (!_options.MessageTopicMap.TryGetValue(type, out var topic))
        {
            throw new InvalidOperationException($"No topic configured for message type {type.FullName}");
        }

        var payload = JsonSerializer.Serialize(message, new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping // for serializing Cyrillic symbols
        });

        var key = Guid.NewGuid().ToString();

        var msg = new Message<string, string> { Key = key, Value = payload };
        await _producer.ProduceAsync(topic, msg, cancellationToken);
    }
}
