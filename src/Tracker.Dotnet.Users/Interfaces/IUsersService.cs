using Tracker.Dotnet.Users.Models;

namespace Tracker.Dotnet.Users.Interfaces
{
    public interface IUsersService
    {
        public Task<IEnumerable<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default);

        public Task<UserDto> GetUserAsync(string login, CancellationToken cancellationToken = default);

        public Task<UserDto> CreateAsync(string login, string displayName, string role, CancellationToken cancellationToken = default);

        public Task<UserDto> EditAsync(string login, string displayName, string role, CancellationToken cancellationToken = default);

        public Task DeactivateAsync(string login, CancellationToken cancellationToken = default);
    }
}
