using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tracker.Dotnet.Auth.Models;
using Tracker.Dotnet.Auth.Services;
using Tracker.Dotnet.Libs.ApiResponse;

namespace Tracker.Dotnet.Auth.Controllers;

[Route("api")]
[ApiController]
[AllowAnonymous]
public class HomeController : ControllerBase
{
    private readonly AuthService _loginService;
    private readonly ILogger<HomeController> _logger;

    public HomeController
        (AuthService loginService,
        ILogger<HomeController> logger)
    {
        _loginService = loginService;
        _logger = logger;
    }
    
    [HttpPost]
    [Route("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login(string login, string password, CancellationToken cancellationToken)
    {
        var result = await _loginService.LoginAsync(login, password, cancellationToken);
        return ApiResponse<LoginResponse>.Success(result);
    }

    [HttpPost]
    [Route("refresh")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Refresh(string refreshToken, CancellationToken cancellationToken)
    {
        // This method is also anonymous because access token may be expired
        var result = await _loginService.RefreshTokenAsync(refreshToken, cancellationToken);
        return ApiResponse<LoginResponse>.Success(result);
    }

    [HttpPost]
    [Route("logout")]
    public async Task<IActionResult> Logout(string refreshToken, CancellationToken cancellationToken)
    {
        await _loginService.LogoutAsync(refreshToken, cancellationToken);
        return Ok();
    }

    [HttpGet]
    [Route("test")]
    public async Task<IActionResult> Test()
    {
        _logger.LogInformation("Test: {parameter}");
        return Ok("Test");
    }
}
