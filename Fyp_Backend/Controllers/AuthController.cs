using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Fyp_Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Fyp_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly Fyp1Context _context;
        private readonly IConfiguration _configuration;

        public AuthController(Fyp1Context context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public class LoginDto
        {
            public string Role { get; set; } = string.Empty;        // "Client" or "Worker"
            public string EmailOrCnic { get; set; } = string.Empty; // Email for Client, CNIC for Worker
            public string Password { get; set; } = string.Empty;
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            try
            {
                if (model.Role == "Client")
                {
                    var client = await _context.Clients.FirstOrDefaultAsync(c => c.Email == model.EmailOrCnic);

                    bool isClientPasswordValid = client != null && client.Password == model.Password;

                    if (client == null || !isClientPasswordValid)
                    {
                        return Unauthorized(new { message = "Invalid credentials." });
                    }

                    var token = GenerateJwtToken(client.ClientId.ToString(), "Client", client.Name);
                    return Ok(new {
                        token,
                        role = "Client",
                        clientId = client.ClientId,
                        email = client.Email,
                        name = client.Name,
                        picture = client.Picture,
                        address = client.Address,
                        phone = client.Phone,
                        message = "Login successful"
                    });
                }
                else if (model.Role == "Worker")
                {
                    var worker = await _context.Workers.FirstOrDefaultAsync(w => w.Cnic == model.EmailOrCnic);
                    bool isPasswordValid = worker != null && worker.Password == model.Password;

                    if (worker == null || !isPasswordValid)
                    {
                        return Unauthorized(new { message = "Invalid credentials." });
                    }

                    var token = GenerateJwtToken(worker.WorkerId.ToString(), "Worker", worker.Name);
                    return Ok(new { 
                        token, 
                        role = "Worker", 
                        workerId = worker.WorkerId, // Explicitly return ID for frontend use
                        message = "Login successful" 
                    });
                }

                return BadRequest(new { message = "Invalid Role." });
            }
            catch (Exception ex)
            {
                // Return the actual error for easier debugging
                return StatusCode(500, new { message = "Error: " + (ex.InnerException?.Message ?? ex.Message) });
            }
        }

        private string GenerateJwtToken(string userId, string role, string name)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(ClaimTypes.Role, role),
                new Claim(ClaimTypes.Name, name),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
