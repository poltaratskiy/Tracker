using Microsoft.AspNetCore.Http;
using Tracker.Dotnet.Libs.RequestContextAccessor.Abstractions;

namespace Tracker.Dotnet.Libs.RequestContextAccessor;

public class RequestContextMiddleware
{
    private readonly RequestDelegate _next;

    public RequestContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(
        HttpContext httpContext,
        IRequestContextAccessor accessor)
    {
        var refId = httpContext.Request.Headers["RefId"].FirstOrDefault() ?? Guid.NewGuid().ToString("N")[^6..];

        accessor.Current = new RequestContext
        {
            RefId = refId,
            JwtToken = GetBearerToken(httpContext)
        };

        try
        {
            await _next(httpContext);
        }
        finally
        {
            accessor.Current = null;
        }
    }

    private static string? GetBearerToken(HttpContext httpContext)
    {
        var auth = httpContext.Request.Headers.Authorization.ToString();
        const string prefix = "Bearer ";

        return auth.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? auth[prefix.Length..]
            : null;
    }
}
