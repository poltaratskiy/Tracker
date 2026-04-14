using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
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
        IRequestContextAccessor accessor, ILogger<RequestContextMiddleware> logger)
    {
        var refId = httpContext.Request.Headers["RefId"].FirstOrDefault() ?? Guid.NewGuid().ToString("N")[^6..];
        var claims = string.Join(", ", httpContext.User.Claims.Select(x => x.Type));
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var fullName = httpContext.User.FindFirst("fullName")?.Value;
        var login = httpContext.User.FindFirst("username")?.Value;
        var role = httpContext.User.FindFirst("roles")?.Value;

        accessor.Current = new RequestContext
        {
            RefId = refId,
            JwtToken = GetBearerToken(httpContext),
            UserId = userId != null ? Guid.Parse(userId) : null,
            FullName = fullName,
            Login = login,
            Role = role,
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
