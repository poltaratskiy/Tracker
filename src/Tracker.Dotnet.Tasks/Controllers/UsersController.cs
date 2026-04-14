using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tracker.Dotnet.Tasks.Application.Pagination;
using Tracker.Dotnet.Tasks.Application.Users.Queries;

namespace Tracker.Dotnet.Tasks.Controllers;

[ApiController]
[Route("[controller]")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize]
    [HttpGet("")]
    public async Task<PagedResponse<UserModel>> GetUsers([FromQuery] GetUsersQuery query)
    {
        return await _mediator.Send(query);
    }
}
