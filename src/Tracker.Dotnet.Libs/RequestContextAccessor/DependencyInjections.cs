using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Tracker.Dotnet.Libs.RequestContextAccessor.Abstractions;

namespace Tracker.Dotnet.Libs.RequestContextAccessor;

public static class DependencyInjections
{
    public static IServiceCollection AddRequestContextAccessor(this IServiceCollection services)
    {
        services.AddSingleton<IRequestContextAccessor, RequestContextAccessor>();
        return services;
    }

    public static IApplicationBuilder UseRequestContext(this IApplicationBuilder app)
    {
        app.UseMiddleware<RequestContextMiddleware>();
        return app;
    }
}
