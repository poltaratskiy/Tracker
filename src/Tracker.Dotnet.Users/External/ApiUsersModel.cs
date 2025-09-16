namespace Tracker.Dotnet.Users.External;

public class ApiUsersModel
{
    public ApiUser[] Users { get; set; } = [];
}

public class ApiUser
{
    public bool Active { get; set; }
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? UserName { get; set; }
    public required string Id { get; set; }
    public RegistrationApi[] Registrations { get; set; } = [];
}

public class RegistrationApi
{
    public required string ApplicationId { get; set; }
    public string[] Roles { get; set; } = [];
}