using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SavingsAPI.Models;
using SavingsAPI.Settings;
using static SavingsAPI.Settings.IToken;

namespace SavingsAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<UserRole> _roleManager;
        private readonly ITokenGenerator _tokenGenerator;
private readonly ILogger _logger;
        public UserController(UserManager<User> userManager, RoleManager<UserRole> roleManager, ILogger logger, ITokenGenerator tokenGenerator)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
            _tokenGenerator = tokenGenerator;
        }
        [Route("signup")]
        public async Task<IActionResult> SignUp([FromBody] User user)
        {
            if (ModelState.IsValid)
            {
                var existUser = await _userManager.FindByEmailAsync(user.Email);

                if (existUser != null)
                {
                    return BadRequest("Error, user with email exists already");
                }

                var newUser = new User
                {
                    FullName = user.Email,
                    UserName = user.UserName,
                    Email = user.Email,
                    Password = user.Password,                   
                };

                var isCreated = await _userManager.CreateAsync(newUser, user.Password);

                if (isCreated.Succeeded)
                {
                    var roleExists = await _roleManager.RoleExistsAsync(user.Roles);
                    if (!roleExists)
                    {
                        await _roleManager.CreateAsync(new UserRole { Name = user.Roles });
                    }
                    await _userManager.AddToRoleAsync(newUser, user.Roles);

                    var tokens = await _tokenGenerator.GenerateJwtToken(newUser);
                    await SaveRefreshTokenAsync(newUser, tokens.RefreshToken);

                    return Ok(new AuthResult
                    {
                        Result = true,
                        ResultToken = tokens.AccessToken,
                        RefreshToken = tokens.RefreshToken
                    });
                }
                return BadRequest(new { Errors = isCreated.Errors.Select(x => x.Description).ToList() });
            }
            return BadRequest("Invalid request payload");
        }
        private async Task SaveRefreshTokenAsync(User user, string refreshToken)
        {
            var userToken = new IdentityUserToken<string>
            {
                UserId = user.Id,
                LoginProvider = "Equity Server",
                Name = "Acess_Token",
                Value = refreshToken
            };
            await _userManager.SetAuthenticationTokenAsync(user, userToken.LoginProvider, userToken.Name, userToken.Value);
        }
    }
}
