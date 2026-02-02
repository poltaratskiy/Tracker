using Tracker.Dotnet.Libs.KafkaAbstractions;

namespace Tracker.Dotnet.Libs.KafkaConsumer;

public class MessageConfigBuilder<TMessage> where TMessage : IMessage
{
    private readonly KafkaConsumerOptions _options;

    public MessageConfigBuilder(KafkaConsumerOptions options)
    {
        _options = options;
    }

    public HandlerConsumerBuilder<TMessage, THandler> Handler<THandler>() where THandler : IHandler<TMessage>
    {
        return new HandlerConsumerBuilder<TMessage, THandler>(_options);
    }
}
