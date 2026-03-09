using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tracker.Dotnet.Libs.KafkaConsumer.Inbox.EFCore;
using Tracker.Dotnet.Libs.KafkaConsumer.Inbox.EFCore.Configuration;
using Tracker.Dotnet.Libs.LoadTests.Configuration;

namespace Tracker.Dotnet.Libs.LoadTests.Persistence;

// This class is nessesary to create migrations without Host
public class InboxDbContextFactory : IDesignTimeDbContextFactory<InboxDbContext>
{
    public InboxDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<InboxDbContext>()
            .UseNpgsql(
                ConfigConstants.GetConnectionString(),
                x => x.MigrationsAssembly(typeof(InboxDbContextFactory).Assembly.FullName))
            .Options;

        var inboxDbConfig = new Action<IServiceProvider, DbContextOptionsBuilder>((sp, db) => db.UseNpgsql(ConfigConstants.GetConnectionString()));

        var efCoreOptions = new EfCoreInboxOptions
        {
            Schema = ConfigConstants.PostgresInboxSchema,
            TableName = ConfigConstants.PostgresInboxTable,
            ConfigureDbContext = inboxDbConfig
        };

        return new InboxDbContext(options, efCoreOptions);
    }
}
