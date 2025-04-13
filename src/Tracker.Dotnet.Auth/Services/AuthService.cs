using Tracker.Dotnet.Auth.Interfaces;
using Tracker.Dotnet.Auth.Models;
using Tracker.Dotnet.Auth.Models.Entities;

namespace Tracker.Dotnet.Auth.Services
{
    public class AuthService
    {
        // There is UserManager to create and manage users, just for notes.
        private IUserService _userService;
        private readonly ITokenGeneratorService _tokenGeneratorService;
        private readonly IRefreshTokenDbService _refreshTokenDbService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUserService userService, 
            ITokenGeneratorService tokenGeneratorService,
            IRefreshTokenDbService refreshTokenDbService,
            IUnitOfWork unitOfWork,
            ILogger<AuthService> logger)
        {
            _userService = userService;
            _tokenGeneratorService = tokenGeneratorService;
            _refreshTokenDbService = refreshTokenDbService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<LoginResponse>> LoginAsync(string login, string password, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Requested log in");
            var user = await _userService.PasswordSignInAsync(login, password, cancellationToken);
            if (user != null)
            {
                _logger.LogInformation("User with login {login} exists and password was correct", login);
                var role = await _userService.GetUserRoleAsync(user, cancellationToken);
                _logger.LogInformation("User's {login} role is {role}", login, role);

                var accessToken = _tokenGeneratorService.GenerateAceessToken(user!, role);
                var refreshToken = _tokenGeneratorService.GenerateRefreshToken();
                var refreshTokenHash = _tokenGeneratorService.GenerateRefreshTokenHash(refreshToken);

                // In our case user can have only 1 active session, so if they logs in again, other sessions become inactive
                var oldTokens = await _refreshTokenDbService.GetActiveRefreshTokensByUserAsync(user!.Id, cancellationToken);
                foreach (var oldToken in oldTokens)
                {
                    oldToken.Status = RefreshTokenStatus.Revoked;
                }

                var refreshTokenDb = new RefreshToken
                {
                    UserId = user.Id,
                    User = user,
                    TokenHash = refreshTokenHash, // storing hashes for security
                    Status = RefreshTokenStatus.Active,
                };

                _refreshTokenDbService.AddRefreshToken(refreshTokenDb);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Access and refresh tokens successfully issued");

                return new Result<LoginResponse>(new LoginResponse(accessToken, refreshToken));
            }

            _logger.LogInformation("User {login} was not authorized", login);
            return new Result<LoginResponse>(401, "Incorrect login or password");
        }

        public async Task<Result<User>> Create(User user, string password, string role)
        {
            var result = await _userService.CreateUserAsync(user, password, role);
            
            if (!result.Success)
            {
                return new Result<User>(400, result.Message!);
            }

            return new Result<User>(user);
        }

        public async Task<Result<LoginResponse>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Requested token refreshing");
            var user = await _userService.FindUserByRefreshTokenAsync(refreshToken, cancellationToken);

            if (user == null)
            {
                _logger.LogInformation("User was not found");
                return new Result<LoginResponse>(401, "User not found");
            }

            // In our case user can have only 1 active session, but foreach is for security reason
            var oldTokens = await _refreshTokenDbService.GetActiveRefreshTokensByUserAsync(user.Id, cancellationToken);
            if (oldTokens.Length > 1)
            {
                _logger.LogWarning("More than 1 active tokens in database for specific user");
            }

            foreach (var oldToken in oldTokens)
            {
                oldToken.Status = RefreshTokenStatus.AlreadyUsed;
            }

            var role = await _userService.GetUserRoleAsync(user, cancellationToken);
            var accessToken = _tokenGeneratorService.GenerateAceessToken(user, role);
            var newRefreshToken = _tokenGeneratorService.GenerateRefreshToken();
            var newRefreshTokenHash = _tokenGeneratorService.GenerateRefreshTokenHash(newRefreshToken);

            var refreshTokenDb = new RefreshToken
            {
                UserId = user.Id,
                User = user,
                TokenHash = newRefreshTokenHash,
                Status = RefreshTokenStatus.Active,
            };

            _refreshTokenDbService.AddRefreshToken(refreshTokenDb);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Token for user {user} was successfully updated", user.UserName);
            return new Result<LoginResponse>(new LoginResponse(accessToken, newRefreshToken));
        }

        public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Requested log out");
            var user = await _userService.FindUserByRefreshTokenAsync(refreshToken, cancellationToken);

            if (user == null)
            {
                _logger.LogInformation("User was not found to log out");
                return;
            }

            var oldTokens = await _refreshTokenDbService.GetActiveRefreshTokensByUserAsync(user.Id, cancellationToken);
            if (oldTokens.Length > 1)
            {
                _logger.LogWarning("More than 1 active tokens in database for specific user while logging out");
            }

            foreach (var oldToken in oldTokens)
            {
                oldToken.Status = RefreshTokenStatus.Revoked;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("User {user} successfully logged out", user.UserName);
        }
    }
}
