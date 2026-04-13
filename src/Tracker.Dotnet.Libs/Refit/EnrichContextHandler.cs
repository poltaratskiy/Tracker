using System.Net.Http.Headers;
using Tracker.Dotnet.Libs.RequestContextAccessor.Abstractions;

namespace Tracker.Dotnet.Libs.Refit;

public class EnrichContextHandler : DelegatingHandler
{
    private readonly IRequestContextAccessor _contextAccessor;

    public EnrichContextHandler(IRequestContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor; 
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var context = _contextAccessor.Current;

        if (context != null)
        {
            request.Headers.Add("RefId", context.RefId);

            if (context.JwtToken != null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.JwtToken);
            }
        }

        return base.SendAsync(request, cancellationToken);
    }
}
