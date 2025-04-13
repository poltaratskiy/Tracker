using Tracker.Dotnet.Auth.Models;
using Tracker.Dotnet.Auth.Models.Entities;

namespace Tracker.Dotnet.Auth.Interfaces
{
    public interface IUserService
    {
        /// <summary>
        /// Checks password and returns User if credentials are correct.
        /// </summary>
        /// <param name="login"> Login </param>
        /// <param name="password"> Password </param>
        /// <param name="cancellationToken"> Cancellation token </param>
        /// <returns> User entity if credentials are correct, null if incorrect </returns>
        public Task<User?> PasswordSignInAsync(string login, string password, CancellationToken cancellationToken);

        public Task<string> GetUserRoleAsync(User user, CancellationToken cancellationToken);

        public Task<Result<User>> CreateUserAsync(User user, string password, string role);

        public Task<User?> FindUserByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);
    }
}
