using Microsoft.AspNetCore.Identity;
using Tracker.Dotnet.Auth.Interfaces;

namespace Tracker.Dotnet.Auth.Services
{
    public class RoleManagerWrapper : IRoleManagerWrapper
    {
        private readonly RoleManager<IdentityRole> _roleManager;

        public RoleManagerWrapper(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }

        public async Task<bool> RoleExistsAsync(string roleName)
        {
            var result = await _roleManager.FindByNameAsync(roleName);
            return result != null;
        }
    }
}
