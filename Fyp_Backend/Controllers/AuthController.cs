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
            public string Role { get; set; } = string.Empty;        // "Client", "Worker", "Company", or "Police"
            public string EmailOrCnic { get; set; } = string.Empty; // Holds Email, CNIC, LicenseNumber, or BadgeID
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
                    return Ok(new
                    {
                        token,
                        role = "Client",
                        clientId = client.ClientId,
                        email = client.Email,
                        name = client.Name,
                        picture = client.Picture,
                        address = client.Address,
                        phone = client.Phone,
                        latitude = client.Latitude,
                        longitude = client.Longitude,
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
                    return Ok(new
                    {
                        token,
                        role = "Worker",
                        workerId = worker.WorkerId,
                        name = worker.Name,
                        picture = worker.Picture,
                        message = "Login successful"
                    });
                }
                else if (model.Role == "Company")
                {
                    var company = await _context.Companies.FirstOrDefaultAsync(c => c.LicenseNumber == model.EmailOrCnic);
                    bool isPasswordValid = company != null && company.Password == model.Password;

                    if (company == null || !isPasswordValid)
                    {
                        return Unauthorized(new { message = "Invalid credentials." });
                    }

                    var token = GenerateJwtToken(company.CompanyID.ToString(), "Company", company.CompanyName);
                    return Ok(new
                    {
                        token,
                        role = "Company",
                        companyId = company.CompanyID,
                        name = company.CompanyName,
                        email = company.Email,
                        phone = company.PhoneNo,
                        address = company.CompanyAddress,
                        licenseNumber = company.LicenseNumber,
                        message = "Login successful"
                    });
                }
                else if (model.Role == "Police")
                {
                    var police = await _context.PoliceOfficers.FirstOrDefaultAsync(p => p.BadgeID == model.EmailOrCnic);
                    bool isPasswordValid = police != null && police.Password == model.Password;

                    if (police == null || !isPasswordValid)
                    {
                        return Unauthorized(new { message = "Invalid credentials." });
                    }

                    var token = GenerateJwtToken(police.PoliceID.ToString(), "Police", police.StationName);
                    return Ok(new
                    {
                        token,
                        role = "Police",
                        policeId = police.PoliceID,
                        stationName = police.StationName,
                        badgeId = police.BadgeID,
                        email = police.Email,
                        phone = police.PhoneNo,
                        address = police.JurisdictionAddress,
                        message = "Login successful"
                    });
                }

                return BadRequest(new { message = "Invalid Role." });
            }
            catch (Exception ex)
            {
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