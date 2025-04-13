namespace Tracker.Dotnet.Auth.Interfaces
{
    public interface IRoleManagerWrapper
    {
        public Task<bool> RoleExistsAsync(string roleName);
    }
}
