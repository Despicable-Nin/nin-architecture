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

                // Generate the refresh token
                // In production, use a cryptographically secure random generator and store this refresh token securely.
                var refreshToken = Guid.NewGuid().ToString().Replace("-", string.Empty).ToUpper();

                var result = new LoginResponse
                {
                    Username = user.NormalizedUserName,
                    Token = new JwtSecurityTokenHandler().WriteToken(token),
                    RefreshToken = refreshToken,
                };

                return Ok(result);

            }

            return BadRequest("Invalid username or password");
        }
        
        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest refreshTokenRequest)
        {
            if (refreshTokenRequest == null || string.IsNullOrWhiteSpace(refreshTokenRequest.RefreshToken))
            {
                return BadRequest("Invalid client request");
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                // Validate the token with lifetime validation disabled so even expired tokens can be used to refresh.
                var principal = tokenHandler.ValidateToken(refreshTokenRequest.RefreshToken, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = configuration["JwtSettings:ValidIssuer"],
                    ValidAudience = configuration["JwtSettings:ValidAudience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JwtSettings:SecretKey"])),
                    ValidateLifetime = false // ignore token expiration during the refresh process
                }, out SecurityToken validatedToken);

                if (!(validatedToken is JwtSecurityToken jwtToken))
                {
                    return Unauthorized("Invalid token");
                }

                // Extract the username (or another claim) from the token. Here, we use ClaimTypes.Name.
                var username = principal.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value;
                if (string.IsNullOrWhiteSpace(username))
                {
                    return Unauthorized("Invalid token payload");
                }

                // Verify that the user exists.
                var user = await userManager.FindByNameAsync(username);
                if (user == null)
                {
                    return Unauthorized("User not found");
                }

                // OPTIONAL: Validate the supplied refresh token against a store here.
                // In production, you would check that the refresh token is valid, has not expired,
                // and matches the one stored for the user.

                // Recreate the claims (or optionally add new claims if needed).
                var roles = await userManager.GetRolesAsync(user);
                List<Claim> claims =
                [
                    new Claim(ClaimTypes.Email, user?.NormalizedEmail!),
                    new Claim(ClaimTypes.NameIdentifier, user?.Id!),
                    new Claim(ClaimTypes.Name, user?.UserName!),
                    new Claim(ClaimTypes.Role, string.Join(",", roles))
                ];

                var jwtIssuer = configuration["JwtSettings:ValidIssuer"];
                var jwtAudience = configuration["JwtSettings:ValidAudience"];
                var jwtSecretKey = configuration["JwtSettings:SecretKey"];
                var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey));

                // Generate a new JWT token.
                var newJwtToken = new JwtSecurityToken(
                    issuer: jwtIssuer,
                    audience: jwtAudience,
                    expires: DateTime.Now.AddHours(2),
                    claims: claims,
                    signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                );

                var newToken = tokenHandler.WriteToken(newJwtToken);

                // Generate a new refresh token.
                // In a real-world scenario, generate a refresh token using a secure random generator
                // and persist it in a secure storage with its expiry time.
                var newRefreshToken = Guid.NewGuid().ToString();

                var response = new RefreshTokenResponse
                {
                    Token = newToken,
                    RefreshToken = newRefreshToken
                };

                return Ok(response);
            }
            catch (Exception)
            {
                return Unauthorized("Invalid refresh token");
            }
        }

        
        
    }
}
