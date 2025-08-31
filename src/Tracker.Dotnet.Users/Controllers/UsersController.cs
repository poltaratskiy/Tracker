using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Tracker.Dotnet.Users.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
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
