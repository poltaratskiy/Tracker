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

        public HomeController
            (AuthService loginService)
        {
            _loginService = loginService;
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
    }
}
