using Microsoft.EntityFrameworkCore;
using Tracker.Dotnet.Auth.Interfaces;
using Tracker.Dotnet.Auth.Models.Entities;
using Tracker.Dotnet.Auth.Persistence;

namespace Tracker.Dotnet.Auth.Services
{
    public class RefreshTokenDbService : IRefreshTokenDbService
    {
        private ApplicationDbContext _context;

        public RefreshTokenDbService(ApplicationDbContext context)
        {
            _context = context;
        }

        public void AddRefreshToken(RefreshToken refreshToken)
        {
            _context.RefreshTokens.Add(refreshToken);
        }

        public Task<RefreshToken[]> GetActiveRefreshTokensByUserAsync(string userId, CancellationToken cancellationToken)
        {
            return _context.RefreshTokens
                .Where(x => x.UserId == userId && x.Status == RefreshTokenStatus.Active)
                .ToArrayAsync(cancellationToken);
        }
    }
}
