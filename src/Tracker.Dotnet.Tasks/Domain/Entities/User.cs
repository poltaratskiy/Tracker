namespace Tracker.Dotnet.Tasks.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public required string Login { get; set; }
    public required string FullName { get; set; }

    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
}
