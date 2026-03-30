using Tracker.Dotnet.Libs.KafkaAbstractions;

namespace Tracker.Dotnet.Libs.LoadTests.ConsumerThroughputTestHandler;

public record ThroughputTestMessage(string Content) : IMessage
{
}
