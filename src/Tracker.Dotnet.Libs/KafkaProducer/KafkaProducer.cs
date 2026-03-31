using Confluent.Kafka;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Tracker.Dotnet.Libs.RequestContextAccessor.Abstractions;

namespace Tracker.Dotnet.Libs.KafkaProducer;

public class KafkaProducer : IKafkaProducer
{
    private readonly IProducerWrapper _producer;
    private readonly KafkaProducerOptions _options;
    private readonly IRequestContextAccessor _requestContextAccessor;

    public KafkaProducer(KafkaProducerOptions options, IProducerWrapper producer, IRequestContextAccessor requestContextAccessor)
    {
        _options = options;
        _requestContextAccessor = requestContextAccessor;

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
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) // for serializing Cyrillic symbols
        });

        var key = Guid.NewGuid().ToString();

        var context = _requestContextAccessor.Current;
        var refId = context?.RefId ?? Guid.NewGuid().ToString("N")[^6..];
        var token = context?.JwtToken;

        var msg = new Message<string, string> 
        { 
            Key = key, 
            Value = payload,
        };

        msg.Headers = new Headers
        {
            { "MessageId", Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()) },
            { "RefId", Encoding.UTF8.GetBytes(refId) },
        };

        if (token != null)
        {
            msg.Headers.Add("Authorization", Encoding.UTF8.GetBytes(token));
        }

        await _producer.ProduceAsync(topic, msg, cancellationToken);
    }
}
