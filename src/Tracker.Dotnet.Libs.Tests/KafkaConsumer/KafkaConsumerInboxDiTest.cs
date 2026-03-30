using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Tracker.Dotnet.Libs.KafkaConsumer;
using Tracker.Dotnet.Libs.KafkaConsumer.Inbox.Abstractions;
using Tracker.Dotnet.Libs.KafkaConsumer.Inbox.Internal;

namespace Tracker.Dotnet.Libs.Tests.KafkaConsumer;

[TestFixture]
public class KafkaConsumerInboxDiTest
{
    [Test]
    public void ConfigureWithoutInbox_Should_NotFail()
    {
        var services = new ServiceCollection();

        services.AddKafkaConsumer(x =>
            x.BootstrapServers("Bootstrap")
            .ConsumerGroup("Group"));

        var provider = services.BuildServiceProvider();
        var inbox = provider.GetRequiredService<IInbox>();
        inbox.ShouldBeOfType<NoOpInbox>();
    }
}
