namespace Tracker.Dotnet.Auth.Configuration
{
    public class JwtConfig
    {
        public required string SymmetricKey { get; set; } = string.Empty;

        public int AccessTokenExpiresMin { get; set; }
    }
}
