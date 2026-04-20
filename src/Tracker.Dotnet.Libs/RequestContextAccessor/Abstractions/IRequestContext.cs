namespace Tracker.Dotnet.Libs.RequestContextAccessor.Abstractions;

public interface IRequestContext
{
    public string? RefId { get; }
    public string? MessageId { get; }
    public Guid? ConsumerInstanceId { get; }
    public Guid? UserId { get; }
    public string? Login { get; }
    public string? FullName { get; }
    public string? Role { get; }
    public string? JwtToken { get; }

    public bool IsInRole(string role);
}
