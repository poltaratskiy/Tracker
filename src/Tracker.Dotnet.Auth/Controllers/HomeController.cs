using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tracker.Dotnet.Auth.Models;
using Tracker.Dotnet.Auth.Services;

namespace Tracker.Dotnet.Auth.Controllers
{
    [Route("api/[controller]")]
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
        [ProducesDefaultResponseType(typeof(Result<LoginResponse>))]
        public async Task<IActionResult> Login(string login, string password, CancellationToken cancellationToken)
        {
            var result = await _loginService.LoginAsync(login, password, cancellationToken);

            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return Unauthorized(result);
            }
        }

        [HttpPost]
        [Route("refresh")]
        [ProducesDefaultResponseType(typeof(Result<LoginResponse>))]
        public async Task<IActionResult> Refresh(string refreshToken, CancellationToken cancellationToken)
        {
            // This method is also anonymous because access token may be expired
            var result = await _loginService.RefreshTokenAsync(refreshToken, cancellationToken);

            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return Unauthorized(result);
            }
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
            _logger.LogInformation("Test: {parameter}", "parameter value");
            return Ok("Test");
        }
    }
}
