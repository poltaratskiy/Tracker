using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OpenIddict.EntityFrameworkCore.Models;
using Tracker.Dotnet.SSO.Api.Domain;

namespace Tracker.Dotnet.SSO.Api.Persistence;

public class ApplicationDbContext : IdentityDbContext<User, IdentityRole, string>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<OpenIddictEntityFrameworkCoreApplication> Applications { get; set; } = default!;
    public DbSet<OpenIddictEntityFrameworkCoreAuthorization> Authorizations { get; set; } = default!;
    public DbSet<OpenIddictEntityFrameworkCoreScope> Scopes { get; set; } = default!;
    public DbSet<OpenIddictEntityFrameworkCoreToken> Tokens { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.UseOpenIddict(); // adds schemas of OpenIddict
    }
}
