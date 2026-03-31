using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;
using Tracker.Dotnet.Libs.RequestContextAccessor.Abstractions;

namespace Tracker.Dotnet.Libs.RefId
{
    /// <summary>
    /// Adds parameter "RefId" to logs output
    /// </summary>
    public class RefIdEnricher : ILogEventEnricher
    {
        private readonly IRequestContextAccessor _contextAccessor;
        private const string PropertyName = "RefId";

        public RefIdEnricher(IRequestContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var context = _contextAccessor.Current;
            var refId = string.Empty;

            if (context?.RefId != null)
            {
                refId = context.RefId;
            }

            var property = propertyFactory.CreateProperty(PropertyName, refId);
            logEvent.AddOrUpdateProperty(property);
        }
    }
}
