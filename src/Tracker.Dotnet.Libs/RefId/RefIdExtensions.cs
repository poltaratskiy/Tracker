using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Tracker.Dotnet.Libs.RefId
{
    public static class RefIdExtensions
    {
        public static IApplicationBuilder UseRefId(this IApplicationBuilder app)
        {
            app.UseMiddleware<RefIdMiddleware>();
            return app;
        }

        public static LoggerConfiguration WithRefId(this LoggerConfiguration config, IServiceProvider services)
        {
            ArgumentNullException.ThrowIfNull(config, nameof(LoggerConfiguration));
            var enricher = new RefIdEnricher(services.GetRequiredService<IHttpContextAccessor>());
            return config.Enrich.With(enricher);
        }
    }
}
