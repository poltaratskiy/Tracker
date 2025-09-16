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

    [AllowAnonymous]
    [HttpGet]
    [Route("anon")]
    public IActionResult GetAnon()
    {
        return Ok();
    }

    [Authorize]
    [HttpGet]
    [Route("auth")]
    public IActionResult GetAuthorized()
    {
        return Ok();
    }

    [Authorize(Roles = "Admin")] // case sensitivity
    [HttpGet]
    [Route("admin")]
    public IActionResult GetAdmin()
    {
        return Ok();
    }

    [Authorize(Roles = "admin")]
    [HttpGet]
    [Route("admin2")]
    public IActionResult GetAdmin2()
    {
        return Ok();
    }

    [Authorize(Roles = "manager")]
    [HttpGet]
    [Route("manager")]
    public IActionResult GetManager()
    {
        return Ok();
    }
}
