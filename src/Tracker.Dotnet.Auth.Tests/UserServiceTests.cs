using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
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
    private Mock<ITokenGeneratorService> _tokenGeneratorServiceMock;
    private ApplicationDbContext _context;

    private UserService _userService;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()) // unique db for every test
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new ApplicationDbContext(options);

        _signInManagerMock = new Mock<ISignInManagerWrapper>();
        _userManagerMock = new Mock<IUserManagerWrapper>();
        _roleManagerMock = new Mock<IRoleManagerWrapper>();
        _tokenGeneratorServiceMock = new Mock<ITokenGeneratorService>();

        _userService = new UserService(
            _signInManagerMock.Object,
            _userManagerMock.Object,
            _roleManagerMock.Object,
            _tokenGeneratorServiceMock.Object,
            _context
        );
    }

    [Test]
    public async Task FindUserByRefreshTokenAsync_ShouldReturnUser_WhenTokenIsValid()
    {
        // Arrange
        var token = "refresh_token";
        var hash = "hashed_token";
        var user = new User { Id = Guid.NewGuid().ToString(), UserName = "test_user" };

        var refreshToken = new RefreshToken
        {
            TokenHash = hash,
            User = user,
            UserId = user.Id,
            Status = RefreshTokenStatus.Active
        };

        _context.Users.Add(user);
        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        _tokenGeneratorServiceMock.Setup(t => t.GenerateRefreshTokenHash(token)).Returns(hash);

        // Act
        var result = await _userService.FindUserByRefreshTokenAsync(token, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(user.Id);
    }

    [Test]
    public async Task CreateUserAsync_ShouldCommit_WhenEverythingIsSuccessful()
    {
        // Arrange
        var user = new User { UserName = "user1" };
        var password = "1234";
        var role = "Admin";

        _roleManagerMock.Setup(r => r.RoleExistsAsync(role)).ReturnsAsync(true);
        _userManagerMock.Setup(u => u.CreateUserAsync(user, password)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(u => u.AddUserToRoleAsync(user, role)).ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _userService.CreateUserAsync(user, password, role);

        // Assert
        result.ShouldBe(user);

        // Check if saved to db
        _context.ChangeTracker.HasChanges().ShouldBeFalse();
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }
}
