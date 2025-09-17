namespace Tracker.Dotnet.Libs.Authorization;

public class JwtConfiguration
{
    public string Authority { get; init; } = null!;
    public string Audience { get; init; } = null!;
    public bool ValidateIssuer { get; init; }
}
