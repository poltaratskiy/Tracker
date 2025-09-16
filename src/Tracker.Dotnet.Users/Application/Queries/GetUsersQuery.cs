using MediatR;
using Tracker.Dotnet.Users.Application.Models;
using Tracker.Dotnet.Users.External;

namespace Tracker.Dotnet.Users.Application.Queries;

public class GetUsersQuery : IRequest<UsersModel>
{
}

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, UsersModel>
{
    private readonly IFusionAuthClient _fusionAuthClient;

    public GetUsersQueryHandler(IFusionAuthClient fusionAuthClient)
    {
        _fusionAuthClient = fusionAuthClient;
    }

    public async Task<UsersModel> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var apiResult = await _fusionAuthClient.GetUsers("*", cancellationToken);
        return new UsersModel
        {
            Users = apiResult.Users
            .Where(x => x.Active)
            .Select(x => new User
            {
                Email = x.Email,
                FullName = x.FirstName,
                UserName = x.UserName,
                Id = x.Id,
                Roles = x.Registrations.SelectMany(r => r.Roles).Distinct().ToArray(),
            }).ToArray(),
        };
    }
}
