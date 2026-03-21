using Microsoft.EntityFrameworkCore;
using Tracker.Dotnet.Libs.KafkaConsumer.Inbox.EFCore.Configuration;
using Tracker.Dotnet.Libs.KafkaConsumer.Inbox.Entities;

namespace Tracker.Dotnet.Libs.KafkaConsumer.Inbox.EFCore;

public abstract class InboxDbContextBase : DbContext
{
    private readonly EfCoreInboxOptions _options;

    public InboxDbContextBase(
        DbContextOptions dbContextOptions,
        EfCoreInboxOptions options)
        : base(dbContextOptions)
    {
        _options = options;
    }

    public DbSet<InboxMessage> InboxMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var entity = modelBuilder.Entity<InboxMessage>();

        if (!string.IsNullOrWhiteSpace(_options.Schema))
        {
            entity.ToTable(_options.TableName, _options.Schema);
        }
        else
        {
            entity.ToTable(_options.TableName);
        }

        entity.HasKey(x => x.MessageId);
    }
}
