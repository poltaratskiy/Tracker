using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using Tracker.Dotnet.Auth.Configuration;
using Tracker.Dotnet.Auth.Models.Entities;
using Tracker.Dotnet.Auth.Services;

namespace Tracker.Dotnet.Auth.Tests;

[TestFixture]
public class TokenGeneratorServiceTests
{
    private TokenGeneratorService _service;
    private JwtConfig _jwtConfig;

    [SetUp]
    public void SetUp()
    {
        _jwtConfig = new JwtConfig
        {
            SymmetricKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)), // min key length for HMAC SHA256
            AccessTokenExpiresMin = 15
        };

        var mockOptions = new Mock<IOptions<JwtConfig>>();
        mockOptions.Setup(x => x.Value).Returns(_jwtConfig);

        _service = new TokenGeneratorService(mockOptions.Object);
    }

    [Test]
    public void GenerateRefreshToken_Should_Return_Valid_Base64_String()
    {
        var token = _service.GenerateRefreshToken();

        token.ShouldNotBeNullOrWhiteSpace();

        // Check if string is valid Base64
        Should.NotThrow(() => Convert.FromBase64String(token));
        Convert.FromBase64String(token).Length.ShouldBe(64);
    }

    [Test]
    public void GenerateRefreshTokenHash_Should_Return_SHA256_Hash()
    {
        var refreshToken = _service.GenerateRefreshToken();
        var hash = _service.GenerateRefreshTokenHash(refreshToken);

        hash.ShouldNotBeNullOrWhiteSpace();
        hash.Length.ShouldBe(44); // SHA256 => 32 bytes => Base64 = 44 symbols
    }

    [Test]
    public void GenerateAccessToken_Should_Contain_Correct_Claims()
    {
        var user = new User
        {
            Id = "123",
            UserName = "testuser"
        };

        var role = "Admin";
        var token = _service.GenerateAceessToken(user, role);

        token.ShouldNotBeNullOrWhiteSpace();

        // Check if can deserialize and get claims
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var claims = jwtToken.Claims.ToList();

        claims.ShouldContain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id);
        claims.ShouldContain(c => c.Type == ClaimTypes.Name && c.Value == user.UserName);
        claims.ShouldContain(c => c.Type == ClaimTypes.Role && c.Value == role);

        jwtToken.ValidTo.ShouldBeGreaterThan(DateTime.UtcNow); // Token is valid
    }
}