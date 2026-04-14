using MediatR;
using Microsoft.EntityFrameworkCore;
using Tracker.Dotnet.Libs.RequestContextAccessor.Abstractions;
using Tracker.Dotnet.Tasks.Application.Pagination;
using Tracker.Dotnet.Tasks.Persistence;

namespace Tracker.Dotnet.Tasks.Application.Users.Queries;

public record GetUsersQuery : IRequest<PagedResponse<UserModel>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public record UserModel(
    Guid Id,
    string Login,
    string FullName,
    string Role);

public class GetUsersHandler : IRequestHandler<GetUsersQuery, PagedResponse<UserModel>>
{
    private readonly ILogger<GetUsersHandler> _logger;
    private readonly ApplicationDbContext _dbContext;
    private readonly IRequestContextAccessor _contextAccessor;

    public GetUsersHandler(ILogger<GetUsersHandler> logger, ApplicationDbContext dbContext, IRequestContextAccessor requestContextAccessor)
    {
        _logger = logger;
        _dbContext = dbContext;
        _contextAccessor = requestContextAccessor;
    }

    public async Task<PagedResponse<UserModel>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var ctx = _contextAccessor.Current;
        _logger.LogInformation("Getting users list");

        var query = _dbContext.Users.Include(x => x.Role).AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderBy(x => x.FullName)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new UserModel(x.Id, x.Login, x.FullName, x.Role.Name))
            .ToArrayAsync(cancellationToken);

        return new PagedResponse<UserModel>(
            users,
            request.Page,
            request.PageSize,
            totalCount);
    }
}
