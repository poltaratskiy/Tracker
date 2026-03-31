using Tracker.Dotnet.Libs.RequestContextAccessor.Abstractions;

namespace Tracker.Dotnet.Libs.RequestContextAccessor;

public class RequestContextAccessor : IRequestContextAccessor
{
    private static readonly AsyncLocal<IRequestContext?> _current = new();

    public IRequestContext? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }
}
