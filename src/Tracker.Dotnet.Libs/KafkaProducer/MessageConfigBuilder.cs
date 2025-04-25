namespace Tracker.Dotnet.Libs.KafkaProducer;

public class MessageConfigBuilder<T>
{
    private readonly KafkaProducerOptions _options;

    public MessageConfigBuilder(KafkaProducerOptions options)
    {
        _options = options;
    }

    public KafkaProducerBuilder Topic(string topic)
    {
        _options.MessageTopicMap[typeof(T)] = topic;
        return new KafkaProducerBuilder(_options);
    }
}
