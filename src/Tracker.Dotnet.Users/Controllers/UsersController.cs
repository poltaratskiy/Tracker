using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tracker.Dotnet.Users.Application.Models;
using Tracker.Dotnet.Users.Application.Queries;

namespace Tracker.Dotnet.Users.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize]
    [HttpGet]
    public async Task<UsersModel> GetUsers(CancellationToken cancellationToken)
    {
        return await _mediator.Send(new GetUsersQuery(), cancellationToken);
    }
}
