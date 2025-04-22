using Microsoft.Extensions.Logging;
using Moq;
using Tracker.Dotnet.Auth.Interfaces;
using Tracker.Dotnet.Auth.Models.Entities;
using Tracker.Dotnet.Auth.Models;
using Tracker.Dotnet.Auth.Services;
using Shouldly;

namespace Tracker.Dotnet.Auth.Tests;

[TestFixture]
public class AuthServiceTests
{
    private Mock<IUserService> _userServiceMock;
    private Mock<ITokenGeneratorService> _tokenGenMock;
    private Mock<IRefreshTokenDbService> _refreshTokenDbMock;
    private Mock<IUnitOfWork> _unitOfWorkMock;
    private Mock<ILogger<AuthService>> _loggerMock;
    private AuthService _authService;

    [SetUp]
    public void SetUp()
    {
        _userServiceMock = new Mock<IUserService>();
        _tokenGenMock = new Mock<ITokenGeneratorService>();
        _refreshTokenDbMock = new Mock<IRefreshTokenDbService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<AuthService>>();

        _authService = new AuthService(
            _userServiceMock.Object,
            _tokenGenMock.Object,
            _refreshTokenDbMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object
        );
    }

    [Test]
    public async Task LoginAsync_Should_Return_Tokens_When_Credentials_Valid()
    {
        var user = new User { Id = "1", UserName = "test" };

        _userServiceMock.Setup(x => x.PasswordSignInAsync("login", "pass", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userServiceMock.Setup(x => x.GetUserRoleAsync(user, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Admin");

        _tokenGenMock.Setup(x => x.GenerateAceessToken(user, "Admin")).Returns("access-token");
        _tokenGenMock.Setup(x => x.GenerateRefreshToken()).Returns("refresh-token");
        _tokenGenMock.Setup(x => x.GenerateRefreshTokenHash("refresh-token")).Returns("hashed");

        _refreshTokenDbMock.Setup(x => x.GetActiveRefreshTokensByUserAsync("1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RefreshToken>());

        var result = await _authService.LoginAsync("login", "pass", CancellationToken.None);

        result.Success.ShouldBeTrue();
        result.Value.AccessToken.ShouldBe("access-token");
        result.Value.RefreshToken.ShouldBe("refresh-token");

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task LoginAsync_Should_Return_401_When_User_Not_Found()
    {
        _userServiceMock.Setup(x => x.PasswordSignInAsync("login", "wrong", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await _authService.LoginAsync("login", "wrong", CancellationToken.None);

        result.Success.ShouldBeFalse();
        result.ErrorCode.ShouldBe(401);
    }

    [Test]
    public async Task Create_Should_Return_User_When_Success()
    {
        var user = new User { Id = "1" };
        _userServiceMock.Setup(x => x.CreateUserAsync(user, "123", "Admin"))
            .ReturnsAsync(new Result<User>(user));

        var result = await _authService.Create(user, "123", "Admin");

        result.Success.ShouldBeTrue();
        result.Value.ShouldBe(user);
    }

    [Test]
    public async Task Create_Should_Return_400_When_Failed()
    {
        var user = new User();
        _userServiceMock.Setup(x => x.CreateUserAsync(user, "123", "Admin"))
            .ReturnsAsync(new Result<User>(400, "bad"));

        var result = await _authService.Create(user, "123", "Admin");

        result.Success.ShouldBeFalse();
        result.ErrorCode.ShouldBe(400);
    }

    [Test]
    public async Task RefreshTokenAsync_Should_Return_New_Tokens()
    {
        var user = new User { Id = "1", UserName = "test" };

        _userServiceMock.Setup(x => x.FindUserByRefreshTokenAsync("old", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _refreshTokenDbMock.Setup(x => x.GetActiveRefreshTokensByUserAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new RefreshToken { Status = RefreshTokenStatus.Active, User = user, UserId = user.Id, TokenHash = "hash" } });

        _userServiceMock.Setup(x => x.GetUserRoleAsync(user, It.IsAny<CancellationToken>()))
            .ReturnsAsync("Admin");
        _tokenGenMock.Setup(x => x.GenerateAceessToken(user, "Admin")).Returns("new-access");
        _tokenGenMock.Setup(x => x.GenerateRefreshToken()).Returns("new-refresh");
        _tokenGenMock.Setup(x => x.GenerateRefreshTokenHash("new-refresh")).Returns("hash");

        var result = await _authService.RefreshTokenAsync("old", CancellationToken.None);

        result.Success.ShouldBeTrue();
        result.Value.AccessToken.ShouldBe("new-access");
        result.Value.RefreshToken.ShouldBe("new-refresh");

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task RefreshTokenAsync_Should_Return_401_If_User_Not_Found()
    {
        _userServiceMock.Setup(x => x.FindUserByRefreshTokenAsync("bad", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await _authService.RefreshTokenAsync("bad", CancellationToken.None);

        result.Success.ShouldBeFalse();
        result.ErrorCode.ShouldBe(401);
    }

    [Test]
    public async Task LogoutAsync_Should_Revoke_Tokens_And_Save()
    {
        var user = new User { Id = "1", UserName = "logout-user" };
        var tokens = new[]
        {
            new RefreshToken { Status = RefreshTokenStatus.Active, User = user, UserId = user.Id, TokenHash = "Hash1" },
            new RefreshToken { Status = RefreshTokenStatus.Active, User = user, UserId = user.Id, TokenHash = "Hash2" }
        };

        _userServiceMock.Setup(x => x.FindUserByRefreshTokenAsync("token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _refreshTokenDbMock.Setup(x => x.GetActiveRefreshTokensByUserAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokens);

        await _authService.LogoutAsync("token", CancellationToken.None);

        tokens.All(t => t.Status == RefreshTokenStatus.Revoked).ShouldBeTrue();
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task LogoutAsync_Should_Do_Nothing_If_User_Not_Found()
    {
        _userServiceMock.Setup(x => x.FindUserByRefreshTokenAsync("invalid", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        await _authService.LogoutAsync("invalid", CancellationToken.None);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
