using Microsoft.Extensions.Logging;
using Moq;
using Tracker.Dotnet.Auth.Interfaces;
using Tracker.Dotnet.Auth.Models.Entities;
using Tracker.Dotnet.Auth.Services;
using Shouldly;
using Tracker.Dotnet.Libs.Exceptions;

namespace Tracker.Dotnet.Auth.Tests;

[TestFixture]
public class AuthServiceTests
{
    private Mock<IUserService> _userServiceMock;
    private Mock<ITokenGeneratorService> _tokenGeneratorServiceMock;
    private Mock<IRefreshTokenDbService> _refreshTokenDbServiceMock;
    private Mock<IUnitOfWork> _unitOfWorkMock;
    private Mock<ILogger<AuthService>> _loggerMock;
    private AuthService _authService;

    [SetUp]
    public void SetUp()
    {
        _userServiceMock = new Mock<IUserService>();
        _tokenGeneratorServiceMock = new Mock<ITokenGeneratorService>();
        _refreshTokenDbServiceMock = new Mock<IRefreshTokenDbService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<AuthService>>();

        _authService = new AuthService(
            _userServiceMock.Object,
            _tokenGeneratorServiceMock.Object,
            _refreshTokenDbServiceMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    [Test]
    public async Task LoginAsync_ShouldReturnTokens_WhenCredentialsAreValid()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var login = "test@example.com";
        var password = "password";
        var user = new User { Id = Guid.NewGuid().ToString(), UserName = login };
        var role = "User";
        var accessToken = "access_token";
        var refreshToken = "refresh_token";
        var refreshTokenHash = "hashed_token";

        _userServiceMock.Setup(s => s.PasswordSignInAsync(login, password, cancellationToken))
            .ReturnsAsync(user);

        _userServiceMock.Setup(s => s.GetUserRoleAsync(user, cancellationToken))
            .ReturnsAsync(role);

        _tokenGeneratorServiceMock.Setup(s => s.GenerateAceessToken(user, role))
            .Returns(accessToken);

        _tokenGeneratorServiceMock.Setup(s => s.GenerateRefreshToken())
            .Returns(refreshToken);

        _tokenGeneratorServiceMock.Setup(s => s.GenerateRefreshTokenHash(refreshToken))
            .Returns(refreshTokenHash);

        _refreshTokenDbServiceMock.Setup(s => s.GetActiveRefreshTokensByUserAsync(user.Id, cancellationToken))
            .ReturnsAsync(Array.Empty<RefreshToken>());

        // Act
        var result = await _authService.LoginAsync(login, password, cancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.AccessToken.ShouldBe(accessToken);
        result.RefreshToken.ShouldBe(refreshToken);

        _refreshTokenDbServiceMock.Verify(s => s.AddRefreshToken(It.Is<RefreshToken>(t => t.TokenHash == refreshTokenHash)));
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(cancellationToken), Times.Once);
    }

    [Test]
    public async Task LoginAsync_ShouldThrow_WhenCredentialsAreInvalid()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var login = "invalid";
        var password = "wrong";

        _userServiceMock.Setup(s => s.PasswordSignInAsync(login, password, cancellationToken))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Should.ThrowAsync<WrongCredentialsException>(() =>
            _authService.LoginAsync(login, password, cancellationToken));
    }

    [Test]
    public async Task RefreshTokenAsync_ShouldReturnNewTokens_WhenRefreshTokenIsValid()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var oldRefreshToken = "old_token";
        var user = new User { Id = Guid.NewGuid().ToString(), UserName = "user" };
        var role = "User";
        var newAccessToken = "new_access_token";
        var newRefreshToken = "new_refresh_token";
        var newRefreshTokenHash = "new_refresh_token_hash";

        _userServiceMock.Setup(s => s.FindUserByRefreshTokenAsync(oldRefreshToken, cancellationToken))
            .ReturnsAsync(user);

        _refreshTokenDbServiceMock.Setup(s => s.GetActiveRefreshTokensByUserAsync(user.Id, cancellationToken))
            .ReturnsAsync(new[] { new RefreshToken { Status = RefreshTokenStatus.Active, User = user, UserId = user.Id, TokenHash = "new_refresh_token_hash" } });

        _userServiceMock.Setup(s => s.GetUserRoleAsync(user, cancellationToken)).ReturnsAsync(role);
        _tokenGeneratorServiceMock.Setup(s => s.GenerateAceessToken(user, role)).Returns(newAccessToken);
        _tokenGeneratorServiceMock.Setup(s => s.GenerateRefreshToken()).Returns(newRefreshToken);
        _tokenGeneratorServiceMock.Setup(s => s.GenerateRefreshTokenHash(newRefreshToken)).Returns(newRefreshTokenHash);

        // Act
        var result = await _authService.RefreshTokenAsync(oldRefreshToken, cancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.AccessToken.ShouldBe(newAccessToken);
        result.RefreshToken.ShouldBe(newRefreshToken);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(cancellationToken), Times.Once);
    }

    [Test]
    public async Task LogoutAsync_ShouldRevokeTokens_WhenUserExists()
    {
        // Arrange
        var token = "refresh_token";
        var user = new User { Id = Guid.NewGuid().ToString(), UserName = "user" };
        var tokens = new[]
        {
            new RefreshToken { Status = RefreshTokenStatus.Active, User = user, UserId = user.Id, TokenHash = "hash" },
            new RefreshToken { Status = RefreshTokenStatus.Active, User = user, UserId = user.Id, TokenHash = "hash" }
        };

        _userServiceMock.Setup(s => s.FindUserByRefreshTokenAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _refreshTokenDbServiceMock.Setup(s => s.GetActiveRefreshTokensByUserAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokens);

        // Act
        await _authService.LogoutAsync(token, CancellationToken.None);

        // Assert
        tokens.ShouldAllBe(t => t.Status == RefreshTokenStatus.Revoked);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Create_ShouldCallUserService()
    {
        // Arrange
        var user = new User { UserName = "newuser" };
        var password = "pass";
        var role = "User";

        _userServiceMock.Setup(s => s.CreateUserAsync(user, password, role))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.Create(user, password, role);

        // Assert
        result.ShouldBe(user);
    }
}
