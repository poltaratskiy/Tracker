using Moq;
using Shouldly;
using Tracker.Dotnet.Users.Application.Queries;
using Tracker.Dotnet.Users.External;

namespace Tracker.Dotnet.Users.Tests;

[TestFixture]
public class GetUsersQueryHandlerTests
{
    [Test]
    public async Task Handle_ReturnsOnlyActiveUsers_MapsFieldsAndDistinctRoles()
    {
        // Arrange
        var api = new Mock<IFusionAuthClient>();

        var apiResponse = new ApiUsersModel
        {
            Users = new[]
            {
                    new ApiUser
                    {
                        Active = true,
                        Email = "a@example.com",
                        FirstName = "Alice",
                        UserName = "alice",
                        Id = "u1",
                        Registrations = new[]
                        {
                            new RegistrationApi { ApplicationId = "app1", Roles = new[] { "Admin", "Manager" } },
                            new RegistrationApi { ApplicationId = "app2", Roles = new[] { "Manager", "User" } }
                        }
                    },
                    new ApiUser
                    {
                        Active = false,
                        Email = "b@example.com",
                        FirstName = "Bob",
                        UserName = "bob",
                        Id = "u2",
                        Registrations = new[]
                        {
                            new RegistrationApi { ApplicationId = "app1", Roles = new[] { "User" } }
                        }
                    }
                }
        };

        api.Setup(x => x.GetUsers(It.IsAny<string>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(apiResponse);

        var sut = new GetUsersQueryHandler(api.Object);

        // Act
        var result = await sut.Handle(new GetUsersQuery(), CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Users.Count().ShouldBe(1, "Must be only active users");

        var u = result.Users.Single();
        u.Id.ShouldBe("u1");
        u.Email.ShouldBe("a@example.com");
        u.UserName.ShouldBe("alice");
        u.FullName.ShouldBe("Alice", "FullName gets from FirstName in current version");
        u.Roles.ShouldBeEquivalentTo(new[] { "Admin", "Manager", "User" },
            "Roles collected from all registration and deduplicated");

        // verify called with "*"
        api.Verify(x => x.GetUsers("*", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_EmptyApiResponse_ReturnsEmptyUsers()
    {
        // Arrange
        var api = new Mock<IFusionAuthClient>();
        api.Setup(x => x.GetUsers(It.IsAny<string>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(new ApiUsersModel { Users = Array.Empty<ApiUser>() });

        var sut = new GetUsersQueryHandler(api.Object);

        // Act
        var result = await sut.Handle(new GetUsersQuery(), CancellationToken.None);

        // Assert
        result.Users.ShouldBeEmpty();
    }

    [Test]
    public async Task Handle_FiltersOutInactiveUsersOnly()
    {
        // Arrange
        var api = new Mock<IFusionAuthClient>();
        api.Setup(x => x.GetUsers(It.IsAny<string>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(new ApiUsersModel
           {
               Users = new[]
               {
                       new ApiUser { Active = false, Id = "x1" },
                       new ApiUser { Active = false, Id = "x2" }
               }
           });

        var sut = new GetUsersQueryHandler(api.Object);

        // Act
        var result = await sut.Handle(new GetUsersQuery(), CancellationToken.None);

        // Assert
        result.Users.ShouldBeEmpty();
    }
}
