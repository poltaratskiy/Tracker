using Tracker.Dotnet.Auth.Models.Entities;

namespace Tracker.Dotnet.Auth.Interfaces
{
    public interface ITokenGeneratorService
    {
        public string GenerateAceessToken(User user, string role);

        public string GenerateRefreshToken();

        public string GenerateRefreshTokenHash(string refreshToken);
    }
}
