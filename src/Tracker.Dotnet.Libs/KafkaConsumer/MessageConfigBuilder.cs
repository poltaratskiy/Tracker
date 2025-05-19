namespace Tracker.Dotnet.Libs.KafkaConsumer;

public class MessageConfigBuilder<TMessage>
{
    private readonly KafkaConsumerOptions _options;

    public MessageConfigBuilder(KafkaConsumerOptions options)
    {
        _options = options;
    }

    public HandlerConsumerBuilder<TMessage, THandler> Handler<THandler>()
    {
        return new HandlerConsumerBuilder<TMessage, THandler>(_options);
    }
}
