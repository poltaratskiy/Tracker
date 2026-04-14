namespace Tracker.Dotnet.Libs.RequestContextAccessor.Abstractions;

internal class RequestContext : IRequestContext
{
    public string? RefId { get; set; }

    public string? MessageId { get; set; }

    public Guid? ConsumerInstanceId { get; set; }

    public Guid? UserId { get; set; }

    public string? Login { get; set; }

    public string? FullName { get; set; }

    public string? Role { get; set; }

    public string? JwtToken { get; set; }
}
