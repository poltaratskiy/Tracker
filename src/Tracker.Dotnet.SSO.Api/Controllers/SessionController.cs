using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Tracker.Dotnet.SSO.Api.Domain;

namespace Tracker.Dotnet.SSO.Api.Controllers;

[ApiController]
[Route("~/api/session")]
public class SessionController : ControllerBase
{
    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _users;

    public SessionController(SignInManager<User> signInManager, UserManager<User> users, IAntiforgery anti)
    {
        _signInManager = signInManager; 
        _users = users; 
    }

    public record LoginDto(string UserName, string Password, bool RememberMe);

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var user = await _users.FindByNameAsync(dto.UserName);
        if (user is null) return Unauthorized();

        var res = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: true);
        if (!res.Succeeded) return Unauthorized();

        await _signInManager.SignInAsync(user, isPersistent: dto.RememberMe, authenticationMethod: "pwd"); // sets .SSO.Auth
        return Ok(new { ok = true });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync(); // removes .SSO.Auth
        return Ok(new { ok = true });
    }

    [HttpGet("me")]
    public IActionResult Me()
        => Ok(new { name = User.Identity?.Name, roles = User.Claims.Where(c => c.Type == "role").Select(c => c.Value) });
}
