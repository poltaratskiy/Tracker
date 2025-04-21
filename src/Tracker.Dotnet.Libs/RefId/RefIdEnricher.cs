using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;

namespace Tracker.Dotnet.Libs.RefId
{
    /// <summary>
    /// Adds parameter "RefId" to logs output
    /// </summary>
    public class RefIdEnricher : ILogEventEnricher
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string PropertyName = "RefId";

        public RefIdEnricher(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var context = _httpContextAccessor.HttpContext;
            var refId = string.Empty;

            if (context?.Items != null && context.Items.TryGetValue("RefId", out var val))
            {
                refId = val as string;
            }

            var property = propertyFactory.CreateProperty(PropertyName, refId);
            logEvent.AddOrUpdateProperty(property);
        }
    }
}
