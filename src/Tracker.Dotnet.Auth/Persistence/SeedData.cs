using Microsoft.AspNetCore.Identity;
using Tracker.Dotnet.Auth.Interfaces;
using Tracker.Dotnet.Auth.Models.Entities;

namespace Tracker.Dotnet.Auth.Persistence;

internal static class SeedData
{
    internal static async Task AddRoles(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<User>>();
        var userService = services.GetRequiredService<IUserService>();

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

        var adminUser = await userManager.FindByNameAsync("admin");
        if (adminUser == null)
        {
            await userService.CreateUserAsync(
            new User
            {
                UserName = "admin"
            }, 
            "123", 
            "Admin");
        }
    }
}
