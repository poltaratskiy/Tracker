using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Extensions.Logging;
using Serilog;
using Shouldly;
using Tracker.Dotnet.Libs.Exceptions;
using Tracker.Dotnet.Users.Models.Entities;
using Tracker.Dotnet.Users.Persistence;
using Tracker.Dotnet.Users.Services;

namespace Tracker.Dotnet.Users.Tests;

[TestFixture]
public class UsersServiceTests
{
    private ApplicationDbContext _context;
    private UsersService _service;
    private ILogger<UsersService> _logger;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);

        var loggerConfig = new LoggerConfiguration()
          .MinimumLevel.Verbose()
          .WriteTo.NUnitOutput()
          .CreateLogger();

        var factory = new SerilogLoggerFactory(loggerConfig);
        _logger = factory.CreateLogger<UsersService>();
        _service = new UsersService(_logger, _context);
    }

    [Test]
    public async Task CreateAsync_Should_Create_User_When_Valid()
    {
        _context.Roles.Add(new Role { Name = "Admin" });
        await _context.SaveChangesAsync();

        var result = await _service.CreateAsync("user1", "User One", "admin");

        result.Login.ShouldBe("user1");
        result.DisplayName.ShouldBe("User One");
        result.Role.ShouldBe("admin");
    }

    [Test]
    public async Task CreateAsync_Should_Throw_When_Login_Exists()
    {
        _context.Roles.Add(new Role { Name = "Admin" });
        _context.Users.Add(new User { Login = "user1", DisplayName = "User One", RoleName = "Admin", IsActive = true });
        await _context.SaveChangesAsync();

        await Should.ThrowAsync<ApiException>(() =>
            _service.CreateAsync("user1", "New User", "Admin"));
    }

    [Test]
    public async Task CreateAsync_Should_Throw_When_Role_Not_Exists()
    {
        await Should.ThrowAsync<ApiException>(() =>
            _service.CreateAsync("user1", "User One", "NoRole"));
    }

    [Test]
    public async Task DeactivateAsync_Should_Set_IsActive_False()
    {
        _context.Roles.Add(new Role { Name = "Admin" });
        _context.Users.Add(new User { Login = "user1", DisplayName = "User One", RoleName = "Admin", IsActive = true });
        await _context.SaveChangesAsync();

        await _service.DeactivateAsync("user1");

        var user = await _context.Users.FirstAsync();
        user.IsActive.ShouldBeFalse();
    }

    [Test]
    public async Task DeactivateAsync_Should_Throw_When_Not_Found()
    {
        await Should.ThrowAsync<ApiException>(() => _service.DeactivateAsync("userX"));
    }

    [Test]
    public async Task EditAsync_Should_Update_DisplayName_And_Role()
    {
        _context.Roles.AddRange(new Role { Name = "Admin" }, new Role { Name = "User" });
        _context.Users.Add(new User { Login = "user1", DisplayName = "Old Name", RoleName = "Admin", IsActive = true });
        await _context.SaveChangesAsync();

        var result = await _service.EditAsync("user1", "New Name", "User");

        result.DisplayName.ShouldBe("New Name");
        result.Role.ShouldBe("User");
    }

    [Test]
    public async Task EditAsync_Should_Throw_When_User_Not_Found()
    {
        _context.Roles.Add(new Role { Name = "Admin" });
        await _context.SaveChangesAsync();

        await Should.ThrowAsync<ApiException>(() => _service.EditAsync("userX", "Name", "Admin"));
    }

    [Test]
    public async Task EditAsync_Should_Throw_When_Role_Not_Found()
    {
        _context.Roles.Add(new Role { Name = "Admin" });
        _context.Users.Add(new User { Login = "user1", DisplayName = "User", RoleName = "Admin", IsActive = true });
        await _context.SaveChangesAsync();

        await Should.ThrowAsync<ApiException>(() => _service.EditAsync("user1", "Name", "MissingRole"));
    }

    [Test]
    public async Task GetUserAsync_Should_Return_UserDto()
    {
        _context.Roles.Add(new Role { Name = "Admin" });
        _context.Users.Add(new User { Login = "user1", DisplayName = "User One", RoleName = "Admin", IsActive = true });
        await _context.SaveChangesAsync();

        var result = await _service.GetUserAsync("user1");

        result.Login.ShouldBe("user1");
    }

    [Test]
    public async Task GetUserAsync_Should_Throw_When_Not_Found()
    {
        await Should.ThrowAsync<ApiException>(() => _service.GetUserAsync("userX"));
    }

    [Test]
    public async Task GetUsersAsync_Should_Return_Only_Active()
    {
        _context.Roles.Add(new Role { Name = "Admin" });
        _context.Users.AddRange(
            new User { Login = "active", DisplayName = "A", RoleName = "Admin", IsActive = true },
            new User { Login = "inactive", DisplayName = "B", RoleName = "Admin", IsActive = false });
        await _context.SaveChangesAsync();

        var result = (await _service.GetUsersAsync()).ToList();

        result.Count.ShouldBe(1);
        result[0].Login.ShouldBe("active");
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }
}
