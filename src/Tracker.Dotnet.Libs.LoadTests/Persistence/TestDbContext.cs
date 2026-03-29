using Microsoft.EntityFrameworkCore;
using Tracker.Dotnet.Libs.KafkaConsumer.Inbox.EFCore;
using Tracker.Dotnet.Libs.KafkaConsumer.Inbox.EFCore.Configuration;
using Tracker.Dotnet.Libs.LoadTests.Persistence.Entities;

namespace Tracker.Dotnet.Libs.LoadTests.Persistence;

public class TestDbContext : InboxDbContextBase
{
    public TestDbContext(DbContextOptions<TestDbContext> dbContextOptions, EfCoreInboxOptions options) : base(dbContextOptions, options)
    {
    }

    /*public TestDbContext(DbContextOptions<TestDbContext> dbContextOptions) : base(dbContextOptions, new EfCoreInboxOptions() { ConfigureDbContext = (sp, db) => new DbContextOptionsBuilder() })
    {
    }*/

    public DbSet<ProcessedMessageInfo> ProcessedMessages { get; set; }
}
