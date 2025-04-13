using Tracker.Dotnet.Auth.Interfaces;
using Tracker.Dotnet.Auth.Models;
using Tracker.Dotnet.Auth.Models.Entities;
using Tracker.Dotnet.Auth.Persistence;

namespace Tracker.Dotnet.Auth.Services
{
    public class UserService : IUserService
    {
        private readonly ISignInManagerWrapper _signInManager;
        private readonly IUserManagerWrapper _userManager;
        private readonly IRoleManagerWrapper _roleManager;
        private readonly ApplicationDbContext _context;

        public UserService(ISignInManagerWrapper signInManager, IUserManagerWrapper userManager, IRoleManagerWrapper roleManager, ApplicationDbContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        public async Task<User?> PasswordSignInAsync(string login, string password, CancellationToken cancellationToken)
        {
            var result = await _signInManager.PasswordSignInAsync(login, password);

            if (!result)
            {
                return null;
            }

            return await _userManager.GetUserByLoginAsync(login);
        }

        public async Task<string> GetUserRoleAsync(User user, CancellationToken cancellationToken)
        {
            var result = await _userManager.GetUserRolesAsync(user);
            return result.First();
        }

        public async Task<Result<User>> CreateUserAsync(User user, string password, string role)
        {
            // Prevent creating user and adding to non-existing role
            if (!await _roleManager.RoleExistsAsync(role))
            {
                return new Result<User>(400, "This role does not exists");
            }

            // As soon as creating user and role assignment are 2 separate operations, for data consistency in case of error it is nessesary to use transaction.
            using var transaction = _context.Database.BeginTransaction();
            var createResult = await _userManager.CreateUserAsync(user, password);

            if (!createResult.Succeeded)
            {
                await transaction.RollbackAsync();
                return new Result<User>(400, string.Join(" ;", createResult.Errors.Select(x => x.Description)));
            }

            var roleResult = await _userManager.AddUserToRoleAsync(user, role);
            
            if (!roleResult.Succeeded)
            {
                await transaction.RollbackAsync();
                return new Result<User>(400, string.Join(" ;", roleResult.Errors.Select(x => x.Description)));
            }

            transaction.Commit();
            return new Result<User>(user); // TODO: Проверить, присваивается ли ID пользователя
        }
    }
}
