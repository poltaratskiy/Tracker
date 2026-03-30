using Tracker.Dotnet.Libs.KafkaAbstractions;

namespace Tracker.Dotnet.Libs.LoadTests.ConsumerLatencyTestHandler;

public record LatencyTestMessage(string Content, DateTime DateSent) : IMessage
{
}
