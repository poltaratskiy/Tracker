using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tracker.Dotnet.Libs.KafkaConsumer.Inbox.EFCore;
using Tracker.Dotnet.Libs.LoadTests.Infrastructure;

namespace Tracker.Dotnet.Libs.LoadTests;

public class SimpleTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    [Ignore("Local only")]
    public async Task Test1()
    {
        await using var env = new TestEnvironment();
        await env.StartAsync();

        var spConfig = new SpConfiguration
        {
            UseTransationalInbox = true,
        };

        var sp = new GenericServiceProvider().GetServiceCollection(spConfig);
        using var scope = sp.CreateScope();
        using var inboxDbContext = scope.ServiceProvider.GetRequiredService<InboxDbContext>();

        var allMigrations = inboxDbContext.Database.GetMigrations();
        Console.WriteLine(string.Join(", ", allMigrations));

        await inboxDbContext.Database.MigrateAsync();

        Assert.Pass();
    }
}
