using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Tracker.Dotnet.Libs.KafkaConsumer;
using Tracker.Dotnet.Libs.KafkaConsumer.Inbox.Abstractions;
using Tracker.Dotnet.Libs.KafkaConsumer.Inbox.EFCore;
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

    [Test]
    public void ConfigureWithInbox_Should_NotFail()
    {
        var services = new ServiceCollection();

        services.AddLogging();

        services.AddKafkaConsumer(x =>
            x.BootstrapServers("Bootstrap")
            .ConsumerGroup("Group"))
            .AddTransactionalInbox(cfg =>
            {
                cfg.Schema = "dbo";
                cfg.TableName = "InboxTbl";
                cfg.ConfigureDbContext = (sp, db) => db.UseInMemoryDatabase("test-db");
            });

        var provider = services.BuildServiceProvider();
        var inbox = provider.GetRequiredService<IInbox>();
        inbox.ShouldBeOfType<EfCoreInbox>();
    }
}
