using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tracker.Dotnet.Libs.ApiResponse;
using Tracker.Dotnet.Users.Interfaces;
using Tracker.Dotnet.Users.Models;

namespace Tracker.Dotnet.Users.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class UsersController : ControllerBase
    {
        private readonly IRolesService _rolesService;
        private readonly IUsersService _usersService;

        public UsersController(IRolesService rolesService, IUsersService usersService)
        {
            _rolesService = rolesService;
            _usersService = usersService;
        }

        [HttpGet]
        [Route("/api/roles")]
        public async Task<ActionResult<ApiResponse<IEnumerable<RoleDto>>>> GetRoles(CancellationToken cancellationToken)
        {
            var roles = await _rolesService.GetRolesAsync(cancellationToken);
            return ApiResponse<IEnumerable<RoleDto>>.Success(roles);
        }

        [HttpGet]
        [Route("")]
        public async Task<ActionResult<ApiResponse<IEnumerable<UserDto>>>> GetUsers(CancellationToken cancellationToken)
        {
            var users = await _usersService.GetUsersAsync(cancellationToken);
            return ApiResponse<IEnumerable<UserDto>>.Success(users);
        }

        [HttpGet]
        [Route("{login}")]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(string login, CancellationToken cancellationToken)
        {
            var user = await _usersService.GetUserAsync(login, cancellationToken);
            return ApiResponse<UserDto>.Success(user);
        }

        [HttpPost]
        [Route("")]
        public async Task<ActionResult<ApiResponse<UserDto>>> Create(string login, string displayName, string role, CancellationToken cancellationToken)
        {
            var user = await _usersService.CreateAsync(login, displayName, role, cancellationToken);
            return ApiResponse<UserDto>.Success(user);
        }

        [HttpPut]
        [Route("{login}")]
        public async Task<ActionResult<ApiResponse<UserDto>>> Edit(string login, string displayName, string role, CancellationToken cancellationToken)
        {
            var user = await _usersService.EditAsync(login, displayName, role, cancellationToken);
            return ApiResponse<UserDto>.Success(user);
        }

        [HttpDelete]
        [Route("{login}")]
        public async Task<IActionResult> Deactivate(string login, CancellationToken cancellationToken)
        {
            await _usersService.DeactivateAsync(login, cancellationToken);
            return Ok();
        }
    }
}
