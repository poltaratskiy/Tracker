namespace Tracker.Dotnet.Users.Models;

public class UserDto
{
    public UserDto(string login, string displayName, string role)
    {
        Login = login;
        DisplayName = displayName;
        Role = role;
    }

    public string Login { get; set; }

    public string DisplayName { get; set; }

    public string Role { get; set; }
}
