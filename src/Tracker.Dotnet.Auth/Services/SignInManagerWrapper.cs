using Microsoft.AspNetCore.Identity;
using Tracker.Dotnet.Auth.Interfaces;
using Tracker.Dotnet.Auth.Models.Entities;

namespace Tracker.Dotnet.Auth.Services
{
    // Wrapper doesn't need unit tests, it is created only because wrapped class doesn't have interface
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public class SignInManagerWrapper : ISignInManagerWrapper
    {
        private readonly SignInManager<User> _signInManager;

        public SignInManagerWrapper(SignInManager<User> signInManager)
        {
            _signInManager = signInManager;
        }

        public async Task<bool> PasswordSignInAsync(string login, string password)
        {
            var result = await _signInManager.PasswordSignInAsync(login, password, false, false);
            return result.Succeeded;
        }
    }
}
