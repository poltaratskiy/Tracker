namespace Tracker.Dotnet.Users.Application.Models;

public class UsersModel
{
    public IEnumerable<User> Users { get; set; } = [];
}

public class User
{
    public required string Id { get; set; }

    public string? Email { get; set; }

    public string? FullName { get; set; }

    public string? UserName { get; set; }

    public IEnumerable<string> Roles { get; set; } = [];
}