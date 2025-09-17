using Microsoft.Extensions.Options;
using Refit;
using Tracker.Dotnet.Users.Configuration;

namespace Tracker.Dotnet.Users.External;

internal static class DependencyExtensions
{
    internal static IServiceCollection AddExternalServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FusionAuthApiConfiguration>(configuration.GetSection("FusionAuthApi"));
        services
            .AddScoped<FusionAuthServiceAuthHandler>()
            .AddRefitClient<IFusionAuthClient>()
            .ConfigureHttpClient((sp, options) =>
            {
                var cfg = sp.GetRequiredService<IOptions<FusionAuthApiConfiguration>>();
                options.BaseAddress = new Uri(cfg.Value.ApiUrl);
                options.Timeout = TimeSpan.FromSeconds(300);
            })
            .AddHttpMessageHandler<FusionAuthServiceAuthHandler>();

        return services;
    }
}
