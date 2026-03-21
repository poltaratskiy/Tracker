using Tracker.Dotnet.Libs.KafkaAbstractions;

namespace Tracker.Dotnet.Libs.LoadTests.ConsumerTestHandler;

public record KafkaTestMessage(string Content) : IMessage
{
}
