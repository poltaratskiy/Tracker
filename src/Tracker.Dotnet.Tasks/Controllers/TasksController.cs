using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tracker.Dotnet.Tasks.Application.Pagination;
using Tracker.Dotnet.Tasks.Application.Tasks.Queries;

namespace Tracker.Dotnet.Tasks.Controllers;

[ApiController]
[Route("[controller]")]
public class TasksController : ControllerBase
{
    private readonly IMediator _mediator;

    public TasksController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize]
    [HttpGet("")]
    public async Task<PagedResponse<TaskModel>> GetTasks([FromQuery] GetTasksQuery query)
    {
        return await _mediator.Send(query);
    }
}
