using Microsoft.EntityFrameworkCore;
using Shouldly;
using Tracker.Dotnet.Users.Models.Entities;
using Tracker.Dotnet.Users.Persistence;
using Tracker.Dotnet.Users.Services;

namespace Tracker.Dotnet.Users.Tests;

[TestFixture]
public class RolesServiceTests
{
    private ApplicationDbContext _context;
    private RolesService _service;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()) // new DB for every test
            .Options;

        _context = new ApplicationDbContext(options);
        _service = new RolesService(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [Test]
    public async Task GetRolesAsync_ShouldReturnAllRoles()
    {
        // Arrange
        _context.Roles.AddRange(
            new Role { Name = "Admin" },
            new Role { Name = "User" });

        await _context.SaveChangesAsync();

        // Act
        var result = (await _service.GetRolesAsync(CancellationToken.None)).ToList();

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldContain(r => r.Name == "Admin");
        result.ShouldContain(r => r.Name == "User");
    }

    [Test]
    public async Task GetRolesAsync_ShouldReturnEmpty_WhenNoRoles()
    {
        // Act
        var result = await _service.GetRolesAsync(CancellationToken.None);

        // Assert
        result.ShouldBeEmpty();
    }
}