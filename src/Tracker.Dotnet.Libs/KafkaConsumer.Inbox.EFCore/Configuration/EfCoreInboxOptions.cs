using Microsoft.EntityFrameworkCore;

namespace Tracker.Dotnet.Libs.KafkaConsumer.Inbox.EFCore.Configuration;

public class EfCoreInboxOptions
{
    public string? Schema  { get; set; }
    public string TableName { get; set; } = "Inbox";

    public Action<IServiceProvider, DbContextOptionsBuilder>? ConfigureDbContext {  get; set; }
}
