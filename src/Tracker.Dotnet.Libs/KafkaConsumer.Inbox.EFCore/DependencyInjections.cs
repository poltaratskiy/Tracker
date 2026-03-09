using Microsoft.Extensions.DependencyInjection;
using Tracker.Dotnet.Libs.KafkaConsumer.Inbox.Abstractions;
using Tracker.Dotnet.Libs.KafkaConsumer.Inbox.EFCore.Configuration;

namespace Tracker.Dotnet.Libs.KafkaConsumer.Inbox.EFCore;

public static class DependencyInjections
{
    public static IServiceCollection AddTransactionalInbox(this IKafkaConsumerConfigurer consumerConfigurer, Action<EfCoreInboxOptions> configure)
    {
        var services = consumerConfigurer.Services;

        var options = new EfCoreInboxOptions();
        configure(options);

        if (options.ConfigureDbContext is null)
            throw new InvalidOperationException("ConfigureDbContext must be configured for Transactional Inbox.");

        services.AddSingleton(options);
        services.AddDbContext<InboxDbContext>((sp, db) =>
        {
            options.ConfigureDbContext(sp, db);
        });

        services.AddScoped<IInbox, EfCoreInbox>();

        return services;
    }
}
