using Microsoft.Extensions.Options;
using Tracker.Dotnet.Users.Configuration;

namespace Tracker.Dotnet.Users.External;

public class FusionAuthServiceAuthHandler : DelegatingHandler
{
    private readonly FusionAuthApiConfiguration _configuration;

    public FusionAuthServiceAuthHandler(IOptions<FusionAuthApiConfiguration> options)
    {
        _configuration = options.Value;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Add("Authorization", _configuration.ApiKey);
        request.Headers.Add("X-FusionAuth-TenantId", _configuration.Tenant);
        return await base.SendAsync(request, cancellationToken);
    }
}
