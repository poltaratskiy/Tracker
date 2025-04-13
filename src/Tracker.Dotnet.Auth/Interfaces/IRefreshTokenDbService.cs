using Tracker.Dotnet.Auth.Models.Entities;

namespace Tracker.Dotnet.Auth.Interfaces
{
    public interface IRefreshTokenDbService
    {
        public Task<RefreshToken[]> GetActiveRefreshTokensByUserAsync(string userId, CancellationToken cancellationToken);

        public void AddRefreshToken(RefreshToken refreshToken);
    }
}
