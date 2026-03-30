using Microsoft.Extensions.DependencyInjection;
using Tracker.Dotnet.Libs.KafkaConsumer.Inbox.Abstractions;
using Tracker.Dotnet.Libs.KafkaConsumer.Inbox.EFCore.Configuration;

namespace Tracker.Dotnet.Libs.KafkaConsumer.Inbox.EFCore;

public static class DependencyInjections
{
    public static IServiceCollection AddTransactionalInbox<TDbContext>(this IKafkaConsumerConfigurer consumerConfigurer, Action<EfCoreInboxOptions> configure)
        where TDbContext : InboxDbContextBase
    {
        var services = consumerConfigurer.Services;

        var options = new EfCoreInboxOptions();
        configure(options);

        if (options.ConfigureDbContext is null)
            throw new InvalidOperationException("ConfigureDbContext must be configured for Transactional Inbox.");

        services.AddSingleton(options);

        services.AddScoped<IInbox, EfCoreInbox<TDbContext>>();

        return services;
    }
}
