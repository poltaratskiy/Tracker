using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using Tracker.Dotnet.SSO.Api.Domain;

namespace Tracker.Dotnet.SSO.Api.Infrastructure;

public class IncludeRolesInTokensHandler : IOpenIddictServerHandler<OpenIddictServerEvents.ProcessSignInContext>
{
    private readonly UserManager<User> _users;
    public IncludeRolesInTokensHandler(UserManager<User> users)
    {
        _users = users;
    }

    public async ValueTask HandleAsync(OpenIddictServerEvents.ProcessSignInContext context)
    {
        var principal = context.Principal!;
        var userId = principal.GetClaim(OpenIddictConstants.Claims.Subject);
        var user = await _users.FindByIdAsync(userId);
        if (user is null) return;

        var roles = await _users.GetRolesAsync(user);
        foreach (var role in roles)
            principal.AddClaim(OpenIddictConstants.Claims.Role, role);

        // notify OpenIddict that claims are for access_token
        foreach (var c in principal.Claims)
        {
            var dest = new List<string>();
            if (c.Type == OpenIddictConstants.Claims.Role ||
                c.Type == OpenIddictConstants.Claims.Name ||
                c.Type == OpenIddictConstants.Claims.Subject)
            {
                dest.Add(OpenIddictConstants.Destinations.AccessToken);
            }
            c.SetDestinations(dest);
        }
    }
}
