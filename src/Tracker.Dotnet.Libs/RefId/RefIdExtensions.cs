using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Tracker.Dotnet.Libs.RequestContextAccessor.Abstractions;

namespace Tracker.Dotnet.Libs.RefId
{
    public static class RefIdExtensions
    {
        public static LoggerConfiguration WithRefId(this LoggerConfiguration config, IServiceProvider services)
        {
            ArgumentNullException.ThrowIfNull(config, nameof(LoggerConfiguration));
            var enricher = new RefIdEnricher(services.GetRequiredService<IRequestContextAccessor>());
            return config.Enrich.With(enricher);
        }
    }
}
