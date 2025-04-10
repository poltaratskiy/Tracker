namespace Tracker.Dotnet.Auth.Models
{
    public class LoginResponse
    {
        public LoginResponse(string accessToken, string refreshToken)
        {
            AccessToken = accessToken;
            RefreshToken = refreshToken;
        }

        public string AccessToken { get; set; }

        public string RefreshToken { get; set; }
    }
}
