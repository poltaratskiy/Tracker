using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tracker.Dotnet.Tasks.Application.Pagination;
using Tracker.Dotnet.Tasks.Application.Tasks.Commands;
using Tracker.Dotnet.Tasks.Application.Tasks.Queries;

namespace Tracker.Dotnet.Tasks.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
public class TasksController : ControllerBase
{
    private readonly IMediator _mediator;

    public TasksController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("")]
    public async Task<PagedResponse<TaskModel>> GetTasks([FromQuery] GetTasksQuery request, CancellationToken cancellationToken)
    {
        return await _mediator.Send(request, cancellationToken);
    }

    [HttpPost("")]
    public async Task<CreatedTaskModel> CreateTask([FromBody] CreateTaskCommand request, CancellationToken cancellationToken)
    {
        return await _mediator.Send(request, cancellationToken);
    }

    [HttpPut("{id}")]
    public async Task UpdateTask(int id, [FromBody] UpdateTaskCommand request, CancellationToken cancellationToken)
    {
        request.Id = id;
        await _mediator.Send(request, cancellationToken);
    }

    [HttpDelete("{id}")]
    public async Task DeleteTask(int id, [FromBody] DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        request.Id = id;
        await _mediator.Send(request, cancellationToken);
    }
}
