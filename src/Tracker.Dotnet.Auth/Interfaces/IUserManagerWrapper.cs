using Microsoft.AspNetCore.Identity;
using Tracker.Dotnet.Auth.Models.Entities;

namespace Tracker.Dotnet.Auth.Interfaces
{
    /// <summary>
    /// UserManager wrapper
    /// </summary>
    public interface IUserManagerWrapper
    {
        public Task<IdentityResult> CreateUserAsync(User user, string password);

        public Task<IdentityResult> AddUserToRoleAsync(User user, string role);

        public Task<User?> GetUserByLoginAsync(string login);

        public Task<IList<string>> GetUserRolesAsync(User user);
    }
}
