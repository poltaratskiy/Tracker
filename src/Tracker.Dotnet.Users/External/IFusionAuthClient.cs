using Refit;

namespace Tracker.Dotnet.Users.External;

public interface IFusionAuthClient
{
    [Get("/api/user/search")]
    public Task<ApiUsersModel> GetUsers(string queryString, CancellationToken cancellationToken);
}
