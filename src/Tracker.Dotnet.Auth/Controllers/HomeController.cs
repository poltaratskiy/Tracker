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
        private readonly LoginService _loginService;

        public HomeController
            (LoginService loginService)
        {
            _loginService = loginService;
        }
        
        [HttpPost]
        [Route("login")]
        [ProducesDefaultResponseType(typeof(Result<LoginResponse>))]
        public async Task<IActionResult> Login(string login, string password)
        {
            var result = await _loginService.Login(login, password);

            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return Unauthorized(result);
            }
        }
    }
}
