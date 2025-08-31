using Microsoft.AspNetCore.Identity;
using Tracker.Dotnet.SSO.Api.Domain;

namespace Tracker.Dotnet.SSO.Persistence;

internal static class SeedData
{
    internal static async Task AddRoles(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<User>>();

        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
        }

        if (!await roleManager.RoleExistsAsync("Manager"))
        {
            await roleManager.CreateAsync(new IdentityRole("Manager"));
        }

        if (!await roleManager.RoleExistsAsync("User"))
        {
            await roleManager.CreateAsync(new IdentityRole("User"));
        }

        if (!await roleManager.RoleExistsAsync("Accountant"))
        {
            await roleManager.CreateAsync(new IdentityRole("Accountant"));
        }

        await Task.Delay(TimeSpan.FromSeconds(1));

        var adminUser = await userManager.FindByNameAsync("Admin");
        if (adminUser == null)
        {
            var user = new User
            {
                UserName = "admin",
            };

            await userManager.CreateAsync(user, "123");
            await userManager.AddToRoleAsync(user, "Admin");
        }
    }
}
