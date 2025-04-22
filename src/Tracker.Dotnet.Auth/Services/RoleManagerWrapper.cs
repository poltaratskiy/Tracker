using Microsoft.AspNetCore.Identity;
using Tracker.Dotnet.Auth.Interfaces;

namespace Tracker.Dotnet.Auth.Services
{
    // Wrapper doesn't need unit tests, it is created only because wrapped class doesn't have interface
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
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
