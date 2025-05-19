namespace Tracker.Dotnet.Libs.KafkaConsumer;

public class HandlerConsumerBuilder<TMessage, THandler>
{
    private readonly KafkaConsumerOptions _options;

    public HandlerConsumerBuilder(KafkaConsumerOptions options)
    {
        _options = options;
    }

    public KafkaConsumerBuilder Topic(string topic)
    {
        _options.ConsumerTopicMap[topic] = (typeof(TMessage), typeof(THandler));
        return new KafkaConsumerBuilder(_options);
    }
}
