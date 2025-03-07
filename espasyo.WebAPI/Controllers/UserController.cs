using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using espasyo.WebAPI.Models.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace espasyo.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, IConfiguration configuration) : ControllerBase
    {

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            
            var user = await userManager.FindByEmailAsync(loginRequest!.Email!);
            if (user != null && await userManager.CheckPasswordAsync(user, loginRequest.Password))
            {
                var roles = await userManager.GetRolesAsync(user);

                List<Claim> claims =
                [
                    new(ClaimTypes.Email, user.NormalizedEmail),
                    new(ClaimTypes.NameIdentifier, user.Id),
                    new(ClaimTypes.Name, user.UserName),
                    new (ClaimTypes.Role, string.Join(",",roles)),
                ];

                var jwtIssuer = configuration["JwtSettings:ValidIssuer"];
                var jwtAudience = configuration["JwtSettings:ValidAudience"];
                var jwtSecretKey = configuration["JwtSettings:SecretKey"];

                var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey));

                var token = new JwtSecurityToken(
                    issuer: jwtIssuer,
                    audience: jwtAudience,
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
