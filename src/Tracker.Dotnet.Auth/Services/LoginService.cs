using Tracker.Dotnet.Auth.Interfaces;
using Tracker.Dotnet.Auth.Models;
using Tracker.Dotnet.Auth.Models.Entities;

namespace Tracker.Dotnet.Auth.Services
{
    public class LoginService
    {
        // There is UserManager to create and manage users, just for notes.
        private readonly ISignInManagerWrapper _signInWrapper;
        private readonly IUserManagerWrapper _userWrapper;
        private readonly ILogger<LoginService> _logger;

        public LoginService(ISignInManagerWrapper signInWrapper, IUserManagerWrapper userWrapper, ILogger<LoginService> logger)
        {
            _signInWrapper = signInWrapper;
            _userWrapper = userWrapper;
            _logger = logger;
        }

        public async Task<Result<LoginResponse>> Login(string login, string password)
        {
            var result = await _signInWrapper.PasswordSignInAsync(login, password);
            if (result)
            {
                return new Result<LoginResponse>(new LoginResponse("access", "refresh"));
            }

            return new Result<LoginResponse>(401, "Incorrect login or password");
        }

        public async Task<Result<int>> Create(User user)
        {
            var result = await _userWrapper.CreateUserAsync(user, "123");
            
            if (!result.Succeeded)
            {
                var errors = string.Join("; ", result.Errors.Select(x => $"Code: {x.Code}, description: {x.Description}").ToArray());
                _logger.LogError("Could not create user, errors: {errors}", errors);

                return new Result<int>(400, "Could not create user, see logs for more details");
            }

            return new Result<int>(Convert.ToInt32(user.Id));
        }
    }
}
