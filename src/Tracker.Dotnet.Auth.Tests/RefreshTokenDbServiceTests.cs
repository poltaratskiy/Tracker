using Microsoft.EntityFrameworkCore;
using Shouldly;
using Tracker.Dotnet.Auth.Interfaces;
using Tracker.Dotnet.Auth.Models.Entities;
using Tracker.Dotnet.Auth.Persistence;
using Tracker.Dotnet.Auth.Services;

namespace Tracker.Dotnet.Auth.Tests
{
    public class RefreshTokenDbServiceTests
    {
        private ApplicationDbContext _context;
        private IRefreshTokenDbService _service;

        [SetUp]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // unique name for tests isolation
                .Options;

            _context = new ApplicationDbContext(options);
            _service = new RefreshTokenDbService(_context);
        }

        [Test]
        public async Task AddRefreshToken_Should_Save_Entity()
        {
            var user = new User();
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _context.RefreshTokens.Count().ShouldBe(0);

            // Arrange
            var token = new RefreshToken
            {
                User = user,
                UserId = user.Id,
                Status = RefreshTokenStatus.Active,
                CreatedAt = DateTime.UtcNow,
                TokenHash = Guid.NewGuid().ToString(),
            };

            // Act
            _service.AddRefreshToken(token);
            await _context.SaveChangesAsync();

            // Assert
            _context.RefreshTokens.Count().ShouldBe(1);
        }

        [Test]
        public async Task GetActiveRefreshTokensByUserAsync_Should_Return_Only_Active_Tokens()
        {
            // Arrange
            var user1 = new User();
            var user2 = new User();
            var user3 = new User();
            _context.Users.AddRange(user1, user2, user3);
            await _context.SaveChangesAsync();

            _context.RefreshTokens.AddRange(
                new RefreshToken { UserId = user1.Id, User = user1, Status = RefreshTokenStatus.Active, TokenHash = Guid.NewGuid().ToString() },
                new RefreshToken { UserId = user1.Id, User = user1, Status = RefreshTokenStatus.AlreadyUsed, TokenHash = Guid.NewGuid().ToString() },
                new RefreshToken { UserId = user1.Id, User = user1, Status = RefreshTokenStatus.Revoked, TokenHash = Guid.NewGuid().ToString() },

                new RefreshToken { UserId = user1.Id, User = user2, Status = RefreshTokenStatus.Active, TokenHash = Guid.NewGuid().ToString() },
                new RefreshToken { UserId = user1.Id, User = user2, Status = RefreshTokenStatus.AlreadyUsed, TokenHash = Guid.NewGuid().ToString() },
                new RefreshToken { UserId = user1.Id, User = user2, Status = RefreshTokenStatus.Revoked, TokenHash = Guid.NewGuid().ToString() },

                new RefreshToken { UserId = user1.Id, User = user3, Status = RefreshTokenStatus.Active, TokenHash = Guid.NewGuid().ToString() },
                new RefreshToken { UserId = user1.Id, User = user3, Status = RefreshTokenStatus.AlreadyUsed, TokenHash = Guid.NewGuid().ToString() },
                new RefreshToken { UserId = user1.Id, User = user3, Status = RefreshTokenStatus.Revoked, TokenHash = Guid.NewGuid().ToString() }
            );
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetActiveRefreshTokensByUserAsync(user2.Id, CancellationToken.None);

            // Assert
            result.Length.ShouldBe(1);

            var resultToken = result[0];
            resultToken.UserId.ShouldBe(user2.Id);
            resultToken.Status.ShouldBe(RefreshTokenStatus.Active);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }
    }
}