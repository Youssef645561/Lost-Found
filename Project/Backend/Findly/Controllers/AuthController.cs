using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using LostFound.BLL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Findly.BLL.Enums;

namespace LostFound.API.Controllers
{
    public record RegisterRequest(string? Username, string? Password, string? Email, string? Phone);
    public record LoginRequest(string? Username, string? Password);

    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _Configuration;

        public AuthController(IConfiguration Configuration)
        {
            this._Configuration = Configuration;
        }

        private string _GenerateJwtToken(clsUser user)
        {
            string key = this._Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is missing");
            string issuer = this._Configuration["Jwt:Issuer"] ?? "LostFound.API";
            string audience = this._Configuration["Jwt:Audience"] ?? "LostFound.Client";
            int expireMinutes = int.TryParse(this._Configuration["Jwt:ExpireMinutes"], out int mins) ? mins : 1440;

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expireMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            string username = request.Username ?? string.Empty, password = request.Password ?? string.Empty, email = request.Email ?? string.Empty;
            string? phone = request.Phone;
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(email))
                return BadRequest(new { message = "Username, password, and email are required." });

            clsUser user = new clsUser
            {
                Username = username.Trim(),
                Email = email.Trim(),
                Phone = phone,
                Role = enUserRole.User
            };

            bool IsRegistered = await user.Register(password);
            if (!IsRegistered)
                return Conflict(new { message = "Registration failed. Username or email may already exist." });

            return StatusCode(201, new
            {
                user.UserID,
                user.Username,
                user.Email,
                user.Phone,
                Role = user.Role.ToString(),
                user.CreatedDate
            });
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            string username = request.Username ?? string.Empty, password = request.Password ?? string.Empty;
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return BadRequest(new { message = "Username and password are required." });

            clsUser? user = await clsUser.Authenticate(username, password);
            if (user == null)
                return Unauthorized(new { message = "Invalid username or password." });

            string token = this._GenerateJwtToken(user);

            return Ok(new
            {
                token,
                user = new
                {
                    user.UserID,
                    user.Username,
                    user.Email,
                    user.Phone,
                    Role = user.Role.ToString(),
                    user.CreatedDate
                }
            });
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            string? userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out int currentUserID))
                return Unauthorized();

            clsUser? user = await clsUser.Find(currentUserID);
            if (user == null)
                return NotFound();

            return Ok(new
            {
                user.UserID,
                user.Username,
                user.Email,
                user.Phone,
                Role = user.Role.ToString(),
                user.CreatedDate
            });
        }
    }
}
