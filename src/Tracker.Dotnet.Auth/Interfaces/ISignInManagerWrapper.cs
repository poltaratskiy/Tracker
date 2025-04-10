namespace Tracker.Dotnet.Auth.Interfaces
{
    /// <summary>
    /// SignInManager wrapper.
    /// </summary>
    public interface ISignInManagerWrapper
    {
        Task<bool> PasswordSignInAsync(string login, string password);
    }
}
