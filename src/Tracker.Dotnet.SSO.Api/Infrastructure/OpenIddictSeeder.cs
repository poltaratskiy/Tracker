using OpenIddict.Abstractions;

namespace Tracker.Dotnet.SSO.Api.Infrastructure;

public class OpenIddictSeeder : IHostedService
{
    private readonly IServiceProvider _sp;
    public OpenIddictSeeder(IServiceProvider sp)
    {
        _sp = sp;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        using var scope = _sp.CreateScope();
        var apps = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        var scopes = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();

        async Task EnsureScope(string name, params string[] resources)
        {
            if (await scopes.FindByNameAsync(name, ct) is null)
            {
                var scope = new OpenIddictScopeDescriptor { Name = name, };
                resources.Select(scope.Resources.Add);
                await scopes.CreateAsync(scope, ct);
            }
                
        }
        await EnsureScope(OpenIddictConstants.Scopes.OpenId);
        await EnsureScope(OpenIddictConstants.Scopes.Profile);
        await EnsureScope(OpenIddictConstants.Scopes.Email);
        await EnsureScope("api.read", "service1");
        await EnsureScope("api.write", "service1");

        // Пример SPA-клиента (public, без секрета)
        if (await apps.FindByClientIdAsync("sso-web", ct) is null)
        {
            await apps.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "sso-web",
                DisplayName = "App SPA",
                ConsentType = OpenIddictConstants.ConsentTypes.Implicit, // without consents
                RedirectUris = { new Uri("http://localhost:5173/callback") },
                PostLogoutRedirectUris = { new Uri("http://localhost:5173/") },
                Permissions =
                {
                    OpenIddictConstants.Permissions.Endpoints.Authorization,
                    OpenIddictConstants.Permissions.Endpoints.Token,
                    OpenIddictConstants.Permissions.Endpoints.Revocation,
                    OpenIddictConstants.Permissions.Endpoints.EndSession,

                    OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                    OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                    OpenIddictConstants.Permissions.ResponseTypes.Code,

                    OpenIddictConstants.Permissions.Scopes.Roles,
                    OpenIddictConstants.Permissions.Scopes.Profile,
                    OpenIddictConstants.Permissions.Scopes.Email,
                    "scp:api.read",
                    "scp:api.write"
                },
                Requirements = { OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange }
            }, ct);
        }
    }
    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
