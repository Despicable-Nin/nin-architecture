using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using espasyo.WebAPI.Models;
using espasyo.WebAPI.Models.User;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace espasyo.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager) : ControllerBase
    {
        private UserManager<IdentityUser> UserManager { get; } = userManager;
        public SignInManager<IdentityUser> SignInManager { get; } = signInManager;

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            var user = await UserManager.FindByEmailAsync(loginRequest!.Email!);
            if (user != null && await UserManager.CheckPasswordAsync(user, loginRequest.Password))
            {
                var roles = await UserManager.GetRolesAsync(user);

                List<Claim> claims =
                [
                    new(ClaimTypes.Email, user.NormalizedEmail),
                    new(ClaimTypes.NameIdentifier, user.Id),
                    new(ClaimTypes.Name, user.UserName),
                    new (ClaimTypes.Role, roles.ToString()),
                ];

                var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Guid.Empty.ToString()));

                var token = new JwtSecurityToken(
                    "espasyo.WebAPI",
                    "espasyo.WebAPI",
                    expires: DateTime.Now.AddHours(2),
                    claims: claims,
                    signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256));

                var result = new LoginResponse
                {
                    Username = user.NormalizedUserName,
                    Token = new JwtSecurityTokenHandler().WriteToken(token)
                };
                
                return Ok(result);

            }

            return BadRequest("Invalid username or password");
        }
        
        
    }
}
