namespace Tracker.Dotnet.Libs.RequestContextAccessor.Abstractions;

public interface IRequestContext
{
    public string? RefId { get; }
    public string? MessageId { get; }
    public Guid? ConsumerInstanceId { get; }
    public string? JwtToken { get; }
}
