using Microsoft.AspNetCore.Identity;
using Tracker.Dotnet.Auth.Interfaces;
using Tracker.Dotnet.Auth.Models.Entities;

namespace Tracker.Dotnet.Auth.Services
{
    // Wrapper doesn't need unit tests, it is created only because wrapped class doesn't have interface
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class UserManagerWrapper : IUserManagerWrapper
    {
        private readonly UserManager<User> _userManager;

        public UserManagerWrapper(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        public Task<IdentityResult> CreateUserAsync(User user, string password)
        {
            return _userManager.CreateAsync(user, password);
        }

        public Task<IdentityResult> AddUserToRoleAsync(User user, string role)
        {
            return _userManager.AddToRoleAsync(user, role);
        }

        public Task<User?> GetUserByLoginAsync(string login)
        {
            return _userManager.FindByNameAsync(login);
        }

        public Task<IList<string>> GetUserRolesAsync(User user)
        {
            return _userManager.GetRolesAsync(user);
        }
    }
}
