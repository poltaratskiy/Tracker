using Microsoft.EntityFrameworkCore;
using Tracker.Dotnet.Users.Interfaces;
using Tracker.Dotnet.Users.Models;
using Tracker.Dotnet.Users.Persistence;

namespace Tracker.Dotnet.Users.Services;

public class RolesService : IRolesService
{
    private readonly ApplicationDbContext _context;

    public RolesService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<RoleDto>> GetRolesAsync(CancellationToken cancellationToken)
    {
        var roles = await _context.Roles.ToArrayAsync(cancellationToken);
        return roles.Select(x => new RoleDto(x.Name)).ToArray();
    }
}
