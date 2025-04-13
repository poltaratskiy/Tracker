using Tracker.Dotnet.Auth.Interfaces;
using Tracker.Dotnet.Auth.Models;
using Tracker.Dotnet.Auth.Models.Entities;

namespace Tracker.Dotnet.Auth.Services
{
    public class LoginService
    {
        // There is UserManager to create and manage users, just for notes.
        private IUserService _userService;
        private readonly ITokenGeneratorService _tokenGeneratorService;
        private readonly IRefreshTokenDbService _refreshTokenDbService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<LoginService> _logger;

        public LoginService(
            IUserService userService, 
            ITokenGeneratorService tokenGeneratorService,
            IRefreshTokenDbService refreshTokenDbService,
            IUnitOfWork unitOfWork,
            ILogger<LoginService> logger)
        {
            _userService = userService;
            _tokenGeneratorService = tokenGeneratorService;
            _refreshTokenDbService = refreshTokenDbService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<LoginResponse>> LoginAsync(string login, string password, CancellationToken cancellationToken)
        {
            var user = await _userService.PasswordSignInAsync(login, password, cancellationToken);
            if (user != null)
            {
                var role = await _userService.GetUserRoleAsync(user, cancellationToken);
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

                return new Result<LoginResponse>(new LoginResponse(accessToken, refreshToken));
            }

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
    }
}
