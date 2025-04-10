using Microsoft.AspNetCore.Identity;
using Tracker.Dotnet.Auth.Interfaces;
using Tracker.Dotnet.Auth.Models.Entities;

namespace Tracker.Dotnet.Auth.Services
{
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
