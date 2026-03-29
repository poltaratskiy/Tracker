using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Tracker.Dotnet.Libs.KafkaConsumer;
using Tracker.Dotnet.Libs.KafkaConsumer.Inbox.EFCore;
using Tracker.Dotnet.Libs.KafkaConsumer.Inbox.EFCore.Configuration;
using Tracker.Dotnet.Libs.KafkaProducer;
using Tracker.Dotnet.Libs.LoadTests.Configuration;
using Tracker.Dotnet.Libs.LoadTests.ConsumerTestHandler;
using Tracker.Dotnet.Libs.LoadTests.Persistence;

namespace Tracker.Dotnet.Libs.LoadTests.Infrastructure;

public class GenericServiceProvider
{
    public ServiceProvider GetConsumerServiceCollection(SpConfiguration configuration, ProcessingCompletitionTracker pct)
    {
        var services = new ServiceCollection();

        AddErrorOnlyLogging(services);
        AddDbContext(services);

        if (configuration.UseTransationalInbox)
        {
            var inboxDbConfig = new Action<IServiceProvider, DbContextOptionsBuilder>((sp, db) => 
                db.UseNpgsql(ConfigConstants.GetConnectionString()));

            services.AddKafkaConsumer(c => 
                c.BootstrapServers(ConfigConstants.KafkaBootstrapServer)
                .ConsumerGroup(ConfigConstants.KafkaConsumerGroup)
                .ForMessage<KafkaTestMessage>().Handler<KafkaTestHandler>().Topic(ConfigConstants.KafkaTopic)
            )
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
                .ConsumerGroup(ConfigConstants.KafkaConsumerGroup)
                .ForMessage<KafkaTestMessage>().Handler<KafkaTestHandler>().Topic(ConfigConstants.KafkaTopic));
        }

        var producerOptions = new KafkaProducerOptions
        {
            Acks = configuration.KafkaAcks,
            BootstrapServers = ConfigConstants.KafkaBootstrapServer,
            Idempotency = configuration.KafkaIdempotency
        };

        services.AddSingleton(producerOptions);

        services.AddSingleton(pct);
        return services.BuildServiceProvider();
    }

    public ServiceProvider GetProducerServiceCollection(SpConfiguration configuration)
    {
        var services = new ServiceCollection();
        AddErrorOnlyLogging(services);
        AddDbContext(services);

        services.AddKafkaProducer(c =>
        {
            c.BootstrapServers(ConfigConstants.KafkaBootstrapServer)
            .Acks(configuration.KafkaAcks)
            .Idempotency(configuration.KafkaIdempotency)
            .ForMessage<KafkaTestMessage>().Topic(ConfigConstants.KafkaTopic);
        });

        return services.BuildServiceProvider();
    }

    public ServiceProvider GetDbServiceCollection()
    {
        var services = new ServiceCollection();
        AddDbContext(services);
        AddErrorOnlyLogging(services);
        return services.BuildServiceProvider();
    }

    private void AddLogging(ServiceCollection services)
    {
        var loggerConfig = new LoggerConfiguration()
           .MinimumLevel.Information()
           .WriteTo.NUnitOutput()
           .CreateLogger();

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(loggerConfig);
        });
    }

    private void AddErrorOnlyLogging(ServiceCollection services)
    {
        var loggerConfig = new LoggerConfiguration()
           .MinimumLevel.Error()
           .WriteTo.NUnitOutput()
           .CreateLogger();

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(loggerConfig);
        });
    }

    private void AddDbContext(ServiceCollection services)
    {
        services.AddDbContext<TestDbContext>((db) =>
        {
            db.UseNpgsql(ConfigConstants.GetConnectionString());
        });

        var options = new EfCoreInboxOptions
        {
            Schema = ConfigConstants.PostgresInboxSchema,
            TableName = ConfigConstants.PostgresInboxTable,
            ConfigureDbContext = (sp, db) => db.UseNpgsql(ConfigConstants.GetConnectionString())
        };

        services.AddSingleton(options);
    }
}
