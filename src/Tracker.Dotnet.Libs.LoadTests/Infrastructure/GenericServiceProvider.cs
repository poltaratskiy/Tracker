using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Tracker.Dotnet.Libs.KafkaConsumer;
using Tracker.Dotnet.Libs.KafkaConsumer.Inbox.EFCore;
using Tracker.Dotnet.Libs.KafkaProducer;
using Tracker.Dotnet.Libs.LoadTests.Configuration;
using Tracker.Dotnet.Libs.LoadTests.Persistence;

namespace Tracker.Dotnet.Libs.LoadTests.Infrastructure;

public class GenericServiceProvider
{
    public ServiceProvider GetServiceCollection(SpConfiguration configuration)
    {
        var services = new ServiceCollection();

        var loggerConfig = new LoggerConfiguration()
           .MinimumLevel.Information()
           .WriteTo.NUnitOutput()
           .CreateLogger();

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(loggerConfig);
        });

        if (configuration.UseTransationalInbox)
        {
            var inboxDbConfig = new Action<IServiceProvider, DbContextOptionsBuilder>((sp, db) => 
                db.UseNpgsql(ConfigConstants.GetConnectionString(), 
                x => x.MigrationsAssembly(typeof(InboxDbContextFactory).Assembly.FullName)));

            services.AddKafkaConsumer(c => c.BootstrapServers(ConfigConstants.KafkaBootstrapServer).ConsumerGroup(ConfigConstants.KafkaConsumerGroup))
                .AddTransactionalInbox(cfg =>
                {
                    cfg.Schema = ConfigConstants.PostgresInboxSchema;
                    cfg.TableName = ConfigConstants.PostgresInboxTable;
                    cfg.ConfigureDbContext = inboxDbConfig;
                });
        }
        else
        {
            services.AddKafkaConsumer(c => 
                c.BootstrapServers(ConfigConstants.KafkaBootstrapServer)
                .ConsumerGroup(ConfigConstants.KafkaConsumerGroup));
        }

        services.AddKafkaProducer(c =>
        {
            c.BootstrapServers(ConfigConstants.KafkaBootstrapServer);
        });

        return services.BuildServiceProvider();
    }
}
