using Confluent.Kafka;
using Microsoft.AspNetCore.Http;
using System.Text;
using System.Text.Json;

namespace Tracker.Dotnet.Libs.KafkaProducer;

public class KafkaProducer : IKafkaProducer
{
    private readonly IProducerWrapper _producer;
    private readonly KafkaProducerOptions _options;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public KafkaProducer(KafkaProducerOptions options, IProducerWrapper producer, IHttpContextAccessor httpContextAccessor)
    {
        _options = options;
        _httpContextAccessor = httpContextAccessor;

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

        string? refId = null;
        var context = _httpContextAccessor.HttpContext;
        if (context?.Items != null && context.Items.TryGetValue("RefId", out var val))
        {
            refId = val as string;
        }

        var msg = new Message<string, string> 
        { 
            Key = key, 
            Value = payload,
        };

        if (!string.IsNullOrEmpty(refId))
        {
            msg.Headers = new Headers
            {
                { "refid", Encoding.UTF8.GetBytes(refId!) }
            };
        }

        await _producer.ProduceAsync(topic, msg, cancellationToken);
    }
}
