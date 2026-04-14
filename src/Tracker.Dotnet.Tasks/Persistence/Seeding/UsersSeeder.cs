using Microsoft.EntityFrameworkCore;
using Tracker.Dotnet.Tasks.Persistence.Entities;

namespace Tracker.Dotnet.Tasks.Persistence.Seeding;

internal class UsersSeeder
{
    private readonly ApplicationDbContext _db;

    internal UsersSeeder(ApplicationDbContext db)
    {
        _db = db;
    }

    internal async Task SeedAsync(CancellationToken cancellationToken)
    {
        await SeedRolesAsync(cancellationToken);
        await SeedUsersAsync(cancellationToken);
    }

    private async Task SeedRolesAsync(CancellationToken cancellationToken)
    {
        if (!await _db.Roles.AnyAsync(x => x.Id == 1, cancellationToken))
        {
            _db.Roles.Add(new Role { Id = 1, Name = "Admin" });
            await _db.SaveChangesAsync(cancellationToken);
        }

        if (!await _db.Roles.AnyAsync(x => x.Id == 2, cancellationToken))
        {
            _db.Roles.Add(new Role { Id = 2, Name = "Manager" });
            await _db.SaveChangesAsync(cancellationToken);
        }

        if (!await _db.Roles.AnyAsync(x => x.Id == 3, cancellationToken))
        {
            _db.Roles.Add(new Role { Id = 3, Name = "User" });
            await _db.SaveChangesAsync(cancellationToken);
        }

        if (!await _db.Roles.AnyAsync(x => x.Id == 4, cancellationToken))
        {
            _db.Roles.Add(new Role { Id = 4, Name = "Accountant" });
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task SeedUsersAsync(CancellationToken cancellationToken)
    {
        // Admins
        if (!await _db.Users.AnyAsync(x => x.Id == Guid.Parse("a1832ad2-8da4-4d47-b447-d7d0a6a52bf2") && x.Login == "admin1", cancellationToken))
        {
            _db.Users.Add(new User { Id = Guid.Parse("a1832ad2-8da4-4d47-b447-d7d0a6a52bf2"), Login = "admin1", FullName = "Admin 1", RoleId = 1 });
            await _db.SaveChangesAsync(cancellationToken);
        }

        if (!await _db.Users.AnyAsync(x => x.Id == Guid.Parse("d3bd787e-51bb-4694-87cb-5d93ea4263ac") && x.Login == "admin2", cancellationToken))
        {
            _db.Users.Add(new User { Id = Guid.Parse("d3bd787e-51bb-4694-87cb-5d93ea4263ac"), Login = "admin2", FullName = "Admin 2", RoleId = 1 });
            await _db.SaveChangesAsync(cancellationToken);
        }

        // Managers
        if (!await _db.Users.AnyAsync(x => x.Id == Guid.Parse("b1792d14-7b17-4b5d-8f82-d3d89e99e9ec") && x.Login == "manager1", cancellationToken))
        {
            _db.Users.Add(new User { Id = Guid.Parse("b1792d14-7b17-4b5d-8f82-d3d89e99e9ec"), Login = "manager1", FullName = "Manager 1", RoleId = 2 });
            await _db.SaveChangesAsync(cancellationToken);
        }

        if (!await _db.Users.AnyAsync(x => x.Id == Guid.Parse("2b150b48-d174-4290-af60-8ace758b921d") && x.Login == "manager2", cancellationToken))
        {
            _db.Users.Add(new User { Id = Guid.Parse("2b150b48-d174-4290-af60-8ace758b921d"), Login = "manager2", FullName = "Manager 2", RoleId = 2 });
            await _db.SaveChangesAsync(cancellationToken);
        }

        // Users
        if (!await _db.Users.AnyAsync(x => x.Id == Guid.Parse("5fdbf766-a86e-4212-b6bd-4dcf2015a2d9") && x.Login == "user1", cancellationToken))
        {
            _db.Users.Add(new User { Id = Guid.Parse("5fdbf766-a86e-4212-b6bd-4dcf2015a2d9"), Login = "user1", FullName = "User 1", RoleId = 3 });
            await _db.SaveChangesAsync(cancellationToken);
        }

        if (!await _db.Users.AnyAsync(x => x.Id == Guid.Parse("f4bb9b78-b444-4cd2-8144-a070148862db") && x.Login == "user2", cancellationToken))
        {
            _db.Users.Add(new User { Id = Guid.Parse("f4bb9b78-b444-4cd2-8144-a070148862db"), Login = "user2", FullName = "User 2", RoleId = 3 });
            await _db.SaveChangesAsync(cancellationToken);
        }

        if (!await _db.Users.AnyAsync(x => x.Id == Guid.Parse("c1c61d92-f2c4-40c9-913e-b82ade9a9671") && x.Login == "user3", cancellationToken))
        {
            _db.Users.Add(new User { Id = Guid.Parse("c1c61d92-f2c4-40c9-913e-b82ade9a9671"), Login = "user3", FullName = "User 3", RoleId = 3 });
            await _db.SaveChangesAsync(cancellationToken);
        }

        // Accountants
        if (!await _db.Users.AnyAsync(x => x.Id == Guid.Parse("3580abf2-c0da-408d-9bb6-f1482703d3fb") && x.Login == "accountant1", cancellationToken))
        {
            _db.Users.Add(new User { Id = Guid.Parse("3580abf2-c0da-408d-9bb6-f1482703d3fb"), Login = "accountant1", FullName = "Accountant 1", RoleId = 4 });
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
