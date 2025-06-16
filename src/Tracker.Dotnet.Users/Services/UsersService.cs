using Microsoft.EntityFrameworkCore;
using Tracker.Dotnet.Libs.Exceptions;
using Tracker.Dotnet.Users.Interfaces;
using Tracker.Dotnet.Users.Models;
using Tracker.Dotnet.Users.Models.Entities;
using Tracker.Dotnet.Users.Persistence;

namespace Tracker.Dotnet.Users.Services;

public class UsersService : IUsersService
{
    private readonly ILogger<UsersService> _logger;
    private readonly ApplicationDbContext _context;

    public UsersService(ILogger<UsersService> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<UserDto> CreateAsync(string login, string displayName, string role, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adding user...");
        if (await _context.Users.AnyAsync(x => x.Login == login))
        {
            throw new ApiException("User with that login has already exists");
        }

        await CheckIfRoleExistsOrThrowAsync(role, cancellationToken);

        var user = new User
        {
            Login = login,
            RoleName = role,
            DisplayName = displayName,
            IsActive = true,
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {login} was added", login);
        return new UserDto(user.Login, user.DisplayName, user.RoleName);
    }

    public async Task DeactivateAsync(string login, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deactivating user...");
        var user = await FindUserOrThrowAsync(login, cancellationToken);
        user.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("User {login} was deactivated successfully", login);
    }

    public async Task<UserDto> EditAsync(string login, string displayName, string role, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Editing user...");
        var user = await FindUserOrThrowAsync(login, cancellationToken);
        await CheckIfRoleExistsOrThrowAsync(role, cancellationToken);
        user.DisplayName = displayName;
        user.RoleName = role;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {login} was edited successfully", login);
        return new UserDto(user.Login, user.DisplayName, user.RoleName);
    }

    public async Task<UserDto> GetUserAsync(string login, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Looking for user...");
        var user = await FindUserOrThrowAsync(login, cancellationToken);
        _logger.LogInformation("User {login} was found", login);
        return new UserDto(user.Login, user.DisplayName, user.RoleName);
    }

    public async Task<IEnumerable<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting all active users...");
        var users = await _context.Users.Where(x => x.IsActive).ToArrayAsync(cancellationToken);
        return users.Select(x => new UserDto(x.Login, x.DisplayName, x.RoleName));
    }

    private async Task<User> FindUserOrThrowAsync(string login, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Login.Equals(login, StringComparison.InvariantCultureIgnoreCase) && x.IsActive, cancellationToken);
        if (user == null)
        {
            throw new ApiException("User not found");
        }

        return user;
    }

    private async Task CheckIfRoleExistsOrThrowAsync(string role, CancellationToken cancellationToken)
    {
        if (!await _context.Roles.AnyAsync(x => x.Name.Equals(role, StringComparison.InvariantCultureIgnoreCase), cancellationToken))
        {
            throw new ApiException("Role does not exists");
        }
    }
}
