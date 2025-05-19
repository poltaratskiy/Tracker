using Microsoft.Extensions.DependencyInjection;

namespace Tracker.Dotnet.Libs.KafkaConsumer;

public static class KafkaConsumerDiExtensions
{
    /// <summary>
    /// Registers Kafka consumer in DI. Set up types and topics, bootstap servers, use IKafkaProducer as a dependency.
    /// </summary>
    /// <exception cref="ArgumentNullException"> Throws if bootstrap servers or consumer group not specified. </exception>"
    /// <exception cref="ApplicationException"> Throws if registered handler does not implements interface IKafkaProducer. </exception>
    /// <param name="services"></param>
    /// <param name="configure"></param>
    /// <returns></returns>
    public static IServiceCollection AddKafkaConsumer(this IServiceCollection services, Action<KafkaConsumerBuilder> configure)
    {
        var options = new KafkaConsumerOptions();
        var builder = new KafkaConsumerBuilder(options);
        configure(builder);

        if (string.IsNullOrEmpty(options.BootstrapServers))
        {
            throw new ArgumentNullException("The bootstrap servers for consumer must be specified");
        }

        if (string.IsNullOrEmpty(options.ConsumerGroup))
        {
            throw new ArgumentNullException("Consumer group for consumer must be specified");
        }

        foreach (var consumer in options.ConsumerTopicMap)
        {
            var interfaces = consumer.Value.HandlerType.GetInterfaces();
            if (!interfaces.Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IKafkaConsumer<>)))
            {
                throw new ApplicationException($"The type {consumer.Value.Item2.Name} does not realize interface IKafkaConsumer");
            }

            services.AddScoped(consumer.Value.HandlerType);
        }

        services.AddSingleton<KafkaConsumerOptions>(options);
        services.AddSingleton<IConsumerWrapper, ConsumerWrapper>();
        services.AddSingleton<IKafkaGeneralConsumer, KafkaGeneralConsumer>();
        services.AddHostedService<KafkaConsumerBackgroundService>();
        return services;
    }
}
