namespace Tracker.Dotnet.Libs.RequestContextAccessor.Abstractions;

public interface IRequestContextAccessor
{
    public IRequestContext? Current { get; set; }
}
