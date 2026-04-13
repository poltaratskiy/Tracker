using Microsoft.Extensions.DependencyInjection;

namespace Tracker.Dotnet.Libs.Refit;

public static class DependencyInjections
{
    public static IHttpClientBuilder AddContextEnricher(this IHttpClientBuilder builder)
    {
        builder.AddHttpMessageHandler<EnrichContextHandler>();
        return builder;
    }
}
