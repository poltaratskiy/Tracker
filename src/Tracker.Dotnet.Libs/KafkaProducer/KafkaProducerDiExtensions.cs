using Microsoft.Extensions.DependencyInjection;

namespace Tracker.Dotnet.Libs.KafkaProducer;

public static class KafkaProducerDiExtensions
{
    /// <summary>
    /// Registers Kafka producer in DI. Set up types and topics, bootstap servers, use IKafkaProducer as a dependency.
    /// </summary>
    /// <exception cref="ArgumentNullException"> Throws if bootstrap servers not specified. </exception>"
    /// <param name="services"></param>
    /// <param name="configure"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static IServiceCollection AddKafkaProducer(this IServiceCollection services, Action<KafkaProducerBuilder> configure)
    {
        var options = new KafkaProducerOptions();
        var builder = new KafkaProducerBuilder(options);
        configure(builder);

        if (string.IsNullOrEmpty(options.BootstrapServers))
        {
            throw new ArgumentNullException("The bootstrap servers for producer must be specified");
        }

        services.AddSingleton(options);
        services.AddSingleton<IKafkaProducer, KafkaProducer>();
        return services;
    }
}
