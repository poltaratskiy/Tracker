using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Tracker.Dotnet.Auth.Configuration;
using Tracker.Dotnet.Auth.Interfaces;
using Tracker.Dotnet.Auth.Models.Entities;

namespace Tracker.Dotnet.Auth.Services
{
    public class TokenGeneratorService : ITokenGeneratorService
    {
        private JwtConfig _jwtConfig;

        public TokenGeneratorService(IOptions<JwtConfig> jwtConfig)
        {
            _jwtConfig = jwtConfig.Value;
        }

        public string GenerateAceessToken(User user, string role)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName!),
                new Claim(ClaimTypes.Role, role),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.SymmetricKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                //issuer: _config["Jwt:Issuer"],        // Sometimes can be nessesary to check issuer and audience but in our case do not need it.
                //audience: _config["Jwt:Audience"],    // Left this commented just as note that it is possible.
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtConfig.AccessTokenExpiresMin),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var randomBytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(randomBytes);
        }

        public string GenerateRefreshTokenHash(string refreshToken)
        {
            var bytes = Encoding.UTF8.GetBytes(refreshToken);
            var hashProvider = SHA256.Create();
            var hash = hashProvider.ComputeHash(bytes);

            var str = Encoding.Default.GetString(hash);
            var strUtf = Encoding.UTF8.GetString(hash);
            var strAscii = Encoding.ASCII.GetString(bytes);

            return strUtf;
        }
    }
}
