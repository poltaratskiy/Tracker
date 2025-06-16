using Tracker.Dotnet.Users.Models;

namespace Tracker.Dotnet.Users.Interfaces;

public interface IRolesService
{
    public Task<IEnumerable<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default);
}
