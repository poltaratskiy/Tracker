using Microsoft.EntityFrameworkCore;

namespace Tracker.Dotnet.Users.Persistence;

internal static class SeedData
{
    internal static async Task Seed(IServiceProvider services)
    {
        await AddRoles(services);
        await AddUsers(services);
    }

    private static async Task AddRoles(IServiceProvider services)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        if (!await context.Roles.AnyAsync())
        {
            context.Roles.Add(new Models.Entities.Role
            {
                Name = "Admin"
            });

            context.Roles.Add(new Models.Entities.Role
            {
                Name = "Manager"
            });

            context.Roles.Add(new Models.Entities.Role
            {
                Name = "User"
            });

            context.Roles.Add(new Models.Entities.Role
            {
                Name = "Accountant"
            });
        }
    }

    private static async Task AddUsers(IServiceProvider services)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        if (!await context.Users.AnyAsync(x => x.Login == "admin"))
        {
            context.Users.Add(new Models.Entities.User
            {
                Login = "admin",
                DisplayName = "Admin",
                IsActive = true,
                RoleName = "Admin"
            });
        }

        if (!await context.Users.AnyAsync(x => x.Login == "manager_bob"))
        {
            context.Users.Add(new Models.Entities.User
            {
                Login = "manager_bob",
                DisplayName = "Manager Bob",
                IsActive = true,
                RoleName = "Manager"
            });
        }

        if (!await context.Users.AnyAsync(x => x.Login == "manager_tony"))
        {
            context.Users.Add(new Models.Entities.User
            {
                Login = "manager_tony",
                DisplayName = "Manager Tony",
                IsActive = true,
                RoleName = "Manager"
            });
        }

        if (!await context.Users.AnyAsync(x => x.Login == "user_david"))
        {
            context.Users.Add(new Models.Entities.User
            {
                Login = "user_david",
                DisplayName = "User David",
                IsActive = true,
                RoleName = "User"
            });
        }

        if (!await context.Users.AnyAsync(x => x.Login == "user_donald"))
        {
            context.Users.Add(new Models.Entities.User
            {
                Login = "user_donald",
                DisplayName = "User Donald",
                IsActive = true,
                RoleName = "User"
            });
        }

        if (!await context.Users.AnyAsync(x => x.Login == "user_john"))
        {
            context.Users.Add(new Models.Entities.User
            {
                Login = "user_john",
                DisplayName = "User John",
                IsActive = true,
                RoleName = "User"
            });
        }

        if (!await context.Users.AnyAsync(x => x.Login == "accountant_paul"))
        {
            context.Users.Add(new Models.Entities.User
            {
                Login = "accountant_paul",
                DisplayName = "Accountant Paul",
                IsActive = true,
                RoleName = "Accountant"
            });
        }

        await context.SaveChangesAsync();
    }
}
