using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tracker.Dotnet.Libs.KafkaConsumer.Inbox.EFCore.Configuration;
using Tracker.Dotnet.Libs.LoadTests.Configuration;

namespace Tracker.Dotnet.Libs.LoadTests.Persistence;

public class TestDbContextFactory : IDesignTimeDbContextFactory<TestDbContext>
{
    public TestDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();

        optionsBuilder.UseNpgsql(ConfigConstants.GetConnectionString());

        var inboxOptions = new EfCoreInboxOptions
        {
            Schema = ConfigConstants.PostgresInboxSchema,
            TableName = ConfigConstants.PostgresInboxTable
        };

        return new TestDbContext(optionsBuilder.Options, inboxOptions);
    }
}
