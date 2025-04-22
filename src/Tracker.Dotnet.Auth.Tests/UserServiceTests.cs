using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Shouldly;
using Tracker.Dotnet.Auth.Interfaces;
using Tracker.Dotnet.Auth.Models.Entities;
using Tracker.Dotnet.Auth.Persistence;
using Tracker.Dotnet.Auth.Services;

namespace Tracker.Dotnet.Auth.Tests;

[TestFixture]
internal class UserServiceTests
{
    private Mock<ISignInManagerWrapper> _signInManagerMock;
    private Mock<IUserManagerWrapper> _userManagerMock;
    private Mock<IRoleManagerWrapper> _roleManagerMock;
    private Mock<ITokenGeneratorService> _tokenGeneratorMock;
    private ApplicationDbContext _context;
    private UserService _service;

    [SetUp]
    public void SetUp()
    {
        _signInManagerMock = new Mock<ISignInManagerWrapper>();
        _userManagerMock = new Mock<IUserManagerWrapper>();
        _roleManagerMock = new Mock<IRoleManagerWrapper>();
        _tokenGeneratorMock = new Mock<ITokenGeneratorService>();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)) // Ignoring transaction warning because InMemory DB doesn't suppurt transactions
            .Options;

        _context = new ApplicationDbContext(options);

        _service = new UserService(
            _signInManagerMock.Object,
            _userManagerMock.Object,
            _roleManagerMock.Object,
            _tokenGeneratorMock.Object,
            _context);
    }

    [Test]
    public async Task PasswordSignInAsync_Should_Return_User_When_Success()
    {
        var user = new User { UserName = "testuser" };

        _signInManagerMock.Setup(s => s.PasswordSignInAsync("test", "pass"))
            .ReturnsAsync(true);

        _userManagerMock.Setup(u => u.GetUserByLoginAsync("test"))
            .ReturnsAsync(user);

        var result = await _service.PasswordSignInAsync("test", "pass", CancellationToken.None);

        result.ShouldBe(user);
    }

    [Test]
    public async Task PasswordSignInAsync_Should_Return_Null_When_Fail()
    {
        _signInManagerMock.Setup(s => s.PasswordSignInAsync("test", "wrong"))
            .ReturnsAsync(false);

        var result = await _service.PasswordSignInAsync("test", "wrong", CancellationToken.None);

        result.ShouldBeNull();
    }

    [Test]
    public async Task GetUserRoleAsync_Should_Return_First_Role()
    {
        var user = new User { UserName = "testuser" };

        _userManagerMock.Setup(x => x.GetUserRolesAsync(user))
            .ReturnsAsync(new List<string> { "Admin", "Editor" });

        var role = await _service.GetUserRoleAsync(user, CancellationToken.None);

        role.ShouldBe("Admin");
    }

    [Test]
    public async Task CreateUserAsync_Should_Fail_If_Role_Does_Not_Exist()
    {
        _roleManagerMock.Setup(x => x.RoleExistsAsync("BadRole")).ReturnsAsync(false);

        var result = await _service.CreateUserAsync(new User(), "123", "BadRole");

        result.Success.ShouldBeFalse();
        result.Message.ShouldBe("This role does not exists");
    }

    [Test]
    public async Task CreateUserAsync_Should_Create_User_And_Assign_Role()
    {
        _roleManagerMock.Setup(x => x.RoleExistsAsync("Admin")).ReturnsAsync(true);
        _userManagerMock.Setup(x => x.CreateUserAsync(It.IsAny<User>(), "pass")).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.AddUserToRoleAsync(It.IsAny<User>(), "Admin")).ReturnsAsync(IdentityResult.Success);

        var result = await _service.CreateUserAsync(new User(), "pass", "Admin");

        result.Success.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
    }

    [Test]
    public async Task CreateUserAsync_Should_Rollback_If_Creation_Fails()
    {
        _roleManagerMock.Setup(x => x.RoleExistsAsync("Admin")).ReturnsAsync(true);
        _userManagerMock.Setup(x => x.CreateUserAsync(It.IsAny<User>(), "pass")).ReturnsAsync(
            IdentityResult.Failed(new IdentityError { Description = "Fail!" }));

        var result = await _service.CreateUserAsync(new User(), "pass", "Admin");

        result.Success.ShouldBeFalse();
        result.Message.ShouldContain("Fail");
    }

    [Test]
    public async Task FindUserByRefreshTokenAsync_Should_Return_User_When_Token_Valid()
    {
        var user = new User { Id = "1", UserName = "Test" };
        var refreshToken = new RefreshToken
        {
            TokenHash = "hashed123",
            Status = RefreshTokenStatus.Active,
            User = user,
            UserId = user.Id,
        };

        _tokenGeneratorMock.Setup(x => x.GenerateRefreshTokenHash("token123")).Returns("hashed123");

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        var result = await _service.FindUserByRefreshTokenAsync("token123", CancellationToken.None);

        result.ShouldBe(user);
    }

    [Test]
    public async Task FindUserByRefreshTokenAsync_Should_Return_Null_When_Token_Invalid()
    {
        var user = new User { Id = "1", UserName = "Test" };
        var refreshToken1 = new RefreshToken
        {
            TokenHash = "hash",
            Status = RefreshTokenStatus.AlreadyUsed,
            User = user,
            UserId = user.Id,
        };

        var refreshToken2 = new RefreshToken
        {
            TokenHash = "hash",
            Status = RefreshTokenStatus.Revoked,
            User = user,
            UserId = user.Id,
        };

        _tokenGeneratorMock.Setup(x => x.GenerateRefreshTokenHash("invalid")).Returns("hash");

        _context.RefreshTokens.AddRange(refreshToken1, refreshToken2);
        await _context.SaveChangesAsync();

        var result = await _service.FindUserByRefreshTokenAsync("invalid", CancellationToken.None);

        result.ShouldBeNull();
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }
}
