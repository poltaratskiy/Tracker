using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Tracker.Dotnet.Libs.RefId
{
    /// <summary>
    /// Just adds pass-through request Id to the context to be available for enrichers.
    /// </summary>
    public class RefIdMiddleware
    {
        private readonly RequestDelegate _next;

        public RefIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var refId = context.Request.Headers.TryGetValue("refid", out var refIdHeader)
                ? refIdHeader.ToString()
                : Guid.NewGuid().ToString("N")[^6..];

            context.Items["RefId"] = refId;
            await _next(context);
        }
    }
}
