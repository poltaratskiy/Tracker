using MediatR;
using Microsoft.EntityFrameworkCore;
using Tracker.Dotnet.Libs.RequestContextAccessor.Abstractions;
using Tracker.Dotnet.Tasks.Application.Pagination;
using Tracker.Dotnet.Tasks.Application.Users.Queries;
using Tracker.Dotnet.Tasks.Domain.Enums;
using Tracker.Dotnet.Tasks.Persistence;

namespace Tracker.Dotnet.Tasks.Application.Tasks.Queries;

public class GetTasksQuery : IRequest<PagedResponse<TaskModel>>
{
    public TaskEntityStatus? Status { get; set; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public record TaskModel(
    int Id,
    string Title,
    string Description,
    decimal Price,
    TaskEntityStatus Status,
    UserModel? Assignee);

public class GetTasksHandler : IRequestHandler<GetTasksQuery, PagedResponse<TaskModel>>
{
    private readonly ILogger<GetTasksHandler> _logger;
    private readonly ApplicationDbContext _dbContext;
    private readonly IRequestContextAccessor _contextAccessor;

    public GetTasksHandler(ILogger<GetTasksHandler> logger, ApplicationDbContext dbContext, IRequestContextAccessor contextAccessor)
    {
        _logger = logger;
        _dbContext = dbContext;
        _contextAccessor = contextAccessor;
    }

    public async Task<PagedResponse<TaskModel>> Handle(GetTasksQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting tasks list");

        var query = _dbContext.Tasks
            .AsNoTracking()
            .Include(x => x.Assignee)
            .OrderByDescending(x => x.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize);

        var context = _contextAccessor.Current;
        if (context!.IsInRole("User"))
        {
            _logger.LogInformation("Current user has role \"User\", applying filters by tasks, assigned to current user or not assigned");
            query = query.Where(x => x.AssigneeId == context.UserId || x.AssigneeId == null);
        }

        if (request.Status != null)
        {
            query = query.Where(x => x.Status == request.Status);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var tasks = await query
            .Select(x => new TaskModel(x.Id, x.Title, x.Description, x.Price, x.Status, x.AssigneeId != null ?
                new UserModel(x.AssigneeId.Value, x.Assignee!.Login, x.Assignee.FullName, x.Assignee.Role.Name) : null))
            .ToArrayAsync(cancellationToken);

        return new PagedResponse<TaskModel>(
            tasks,
            request.Page,
            request.PageSize,
            totalCount);
    }
}

