using Tracker.Dotnet.Libs.KafkaAbstractions;

namespace Tracker.Dotnet.Libs.KafkaConsumer;

public class HandlerConsumerBuilder<TMessage, THandler> 
    where TMessage : IMessage 
    where THandler : IHandler<TMessage>
{
    private readonly KafkaConsumerOptions _options;

    public HandlerConsumerBuilder(KafkaConsumerOptions options)
    {
        _options = options;
    }

    public KafkaConsumerBuilder Topic(string topic)
    {
        _options.ConsumerTopicMap[topic] = new MessageHandlerMap(typeof(TMessage), typeof(THandler));
        return new KafkaConsumerBuilder(_options);
    }
}
