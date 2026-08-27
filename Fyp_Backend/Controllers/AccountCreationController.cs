using BCrypt.Net;
using Fyp_Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Fyp_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountCreationController : ControllerBase
    {
        private readonly Fyp1Context _context;
        private readonly IWebHostEnvironment _environment;

        public AccountCreationController(Fyp1Context context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpPost("SignupClient")]
        public async Task<IActionResult> SignupClient([FromForm] Client model)
        {
            try
            {
                if (await _context.Clients.AnyAsync(c => c.Email == model.Email))
                    return BadRequest(new { message = "Email is already registered." });

                string safeFileName = model.Email.Replace("@", "_").Replace(".", "_");
                string imagePath = await SaveImage(model.PictureFile, safeFileName);

                if (imagePath == "Invalid") return BadRequest(new { message = "Only .jpg, .jpeg, and .png files are allowed." });

                model.Picture = imagePath;
                _context.Clients.Add(model);
                await _context.SaveChangesAsync();

                return Ok(new { status = "Success", message = "Client registered successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error: " + (ex.InnerException?.Message ?? ex.Message) });
            }
        }

        [HttpPost("UpdateClient")]
        public async Task<IActionResult> UpdateClient([FromForm] Client model)
        {
            try
            {
                var existingClient = await _context.Clients.FindAsync(model.ClientId);
                if (existingClient == null)
                    return NotFound(new { message = "Client not found." });

                if (model.PictureFile != null)
                {
                    string safeFileName = model.Email.Replace("@", "_").Replace(".", "_");
                    string imagePath = await SaveImage(model.PictureFile, safeFileName);
                    if (imagePath != "Invalid" && imagePath != null)
                    {
                        existingClient.Picture = imagePath;
                    }
                }

                existingClient.Name = model.Name;
                existingClient.Phone = model.Phone;
                existingClient.Address = model.Address;
                existingClient.Email = model.Email;

                if (!string.IsNullOrEmpty(model.Password) && model.Password != "********")
                {
                    existingClient.Password = model.Password;
                }

                await _context.SaveChangesAsync();
                return Ok(new { status = "Success", message = "Profile updated successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Update failed: " + (ex.InnerException?.Message ?? ex.Message) });
            }
        }

        [HttpPost("SignupWorker")]
        public async Task<IActionResult> SignupWorker([FromForm] Worker model, [FromForm] string experiencesJson)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (await _context.Workers.AnyAsync(w => w.Cnic == model.Cnic))
                    return BadRequest(new { message = "CNIC is already registered." });

                string imagePath = await SaveImage(model.PictureFile, model.Cnic);
                if (imagePath == "Invalid") return BadRequest(new { message = "Invalid image." });

                if (imagePath != null)
                {
                    model.Picture = imagePath;
                }
                model.AvailableStatus = true;

                _context.Workers.Add(model);
                await _context.SaveChangesAsync();

                if (!string.IsNullOrEmpty(experiencesJson))
                {
                    var experiences = JsonConvert.DeserializeObject<List<Experience>>(experiencesJson);
                    if (experiences != null)
                    {
                        var uniqueJunctions = new HashSet<(int, int)>();
                        foreach (var exp in experiences)
                        {
                            exp.Worker = null;
                            exp.WorkerId = model.WorkerId;
                            exp.ExperienceId = 0;

                            _context.Experiences.Add(exp);

                            int catId = exp.CategoryId ?? 0;
                            int skillId = exp.SkillsId ?? 0;

                            if (catId > 0 && skillId > 0 && !uniqueJunctions.Contains((catId, skillId)))
                            {
                                uniqueJunctions.Add((catId, skillId));
                                _context.WorkerCategories.Add(new WorkerCategory
                                {
                                    WorkerId = model.WorkerId,
                                    CategoryId = catId,
                                    SkillsId = skillId
                                });
                            }
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                return Ok(new { status = "Success", message = "Profile and Skills created!" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPost("UpdateWorker")]
        public async Task<IActionResult> UpdateWorker([FromForm] Worker model, [FromForm] string experiencesJson)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existingWorker = await _context.Workers.FindAsync(model.WorkerId);
                if (existingWorker == null)
                    return NotFound(new { message = "Worker not found." });

                if (model.PictureFile != null)
                {
                    string identifier = model.Cnic ?? existingWorker.Cnic;
                    string imagePath = await SaveImage(model.PictureFile, identifier);
                    if (imagePath != "Invalid" && imagePath != null)
                    {
                        existingWorker.Picture = imagePath;
                    }
                }

                existingWorker.Name = model.Name;
                existingWorker.Phone = model.Phone;
                existingWorker.Address = model.Address;
                existingWorker.Age = model.Age;
                existingWorker.Salary = model.Salary;
                existingWorker.Gender = model.Gender;
                existingWorker.Bio = model.Bio;

                if (!string.IsNullOrEmpty(model.Password) && model.Password != "********")
                {
                    existingWorker.Password = model.Password;
                }

                if (!string.IsNullOrEmpty(experiencesJson))
                {
                    var oldExps = await _context.Experiences.Where(e => e.WorkerId == model.WorkerId).ToListAsync();
                    _context.Experiences.RemoveRange(oldExps);

                    var oldCats = await _context.WorkerCategories.Where(wc => wc.WorkerId == model.WorkerId).ToListAsync();
                    _context.WorkerCategories.RemoveRange(oldCats);

                    await _context.SaveChangesAsync();

                    var experiences = JsonConvert.DeserializeObject<List<Experience>>(experiencesJson);
                    if (experiences != null)
                    {
                        var uniqueJunctions = new HashSet<(int, int)>();

                        foreach (var exp in experiences)
                        {
                            exp.WorkerId = model.WorkerId;
                            exp.ExperienceId = 0;
                            _context.Experiences.Add(exp);

                            int catId = exp.CategoryId ?? 0;
                            int skillId = exp.SkillsId ?? 0;

                            if (catId > 0 && skillId > 0 && !uniqueJunctions.Contains((catId, skillId)))
                            {
                                uniqueJunctions.Add((catId, skillId));
                                _context.WorkerCategories.Add(new WorkerCategory
                                {
                                    WorkerId = model.WorkerId,
                                    CategoryId = catId,
                                    SkillsId = skillId
                                });
                            }
                        }
                        await _context.SaveChangesAsync();
                    }
                }

                await transaction.CommitAsync();
                return Ok(new { status = "Success", message = "Profile updated successfully!" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Update failed: " + (ex.InnerException?.Message ?? ex.Message) });
            }
        }

        [HttpPost("SignupCompany")]
        public async Task<IActionResult> SignupCompany([FromForm] Company model, [FromForm] IFormFile? LogoFile)
        {
            try
            {
                if (await _context.Companies.AnyAsync(c => c.Email == model.Email))
                    return BadRequest(new { message = "Email is already registered." });

                if (await _context.Companies.AnyAsync(c => c.LicenseNumber == model.LicenseNumber))
                    return BadRequest(new { message = "License/Registration number is already registered." });

                string safeFileName = "company_" + model.Email.Replace("@", "_").Replace(".", "_");
                string imagePath = await SaveImage(LogoFile, safeFileName);

                if (imagePath == "Invalid")
                    return BadRequest(new { message = "Only .jpg, .jpeg, and .png files are allowed." });

                if (imagePath != null)
                {
                    model.CompanyPicture = imagePath;
                }

                model.CreatedAt = DateTime.Now;
                _context.Companies.Add(model);
                await _context.SaveChangesAsync();

                return Ok(new { status = "Success", message = "Company registered successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error: " + (ex.InnerException?.Message ?? ex.Message) });
            }
        }

        [HttpPost("UpdateCompany")]
        public async Task<IActionResult> UpdateCompany([FromForm] Company model, [FromForm] IFormFile? LogoFile)
        {
            try
            {
                var existingCompany = await _context.Companies.FindAsync(model.CompanyID);
                if (existingCompany == null)
                    return NotFound(new { message = "Company profile not found." });

                if (LogoFile != null)
                {
                    string safeFileName = "company_" + model.Email.Replace("@", "_").Replace(".", "_");
                    string imagePath = await SaveImage(LogoFile, safeFileName);
                    if (imagePath != "Invalid" && imagePath != null)
                    {
                        existingCompany.CompanyPicture = imagePath;
                    }
                }

                existingCompany.CompanyName = model.CompanyName;
                existingCompany.PhoneNo = model.PhoneNo;
                existingCompany.CompanyAddress = model.CompanyAddress;
                existingCompany.LicenseNumber = model.LicenseNumber;
                existingCompany.Email = model.Email;

                if (!string.IsNullOrEmpty(model.Password) && model.Password != "********")
                {
                    existingCompany.Password = model.Password;
                }

                await _context.SaveChangesAsync();
                return Ok(new { status = "Success", message = "Company profile updated successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Update failed: " + (ex.InnerException?.Message ?? ex.Message) });
            }
        }

        // ==========================================
        // NEW: POLICE OFFICER SIGNUP & UPDATE ENDPOINTS
        // ==========================================

        [HttpPost("SignupPolice")]
        public async Task<IActionResult> SignupPolice([FromForm] PoliceOfficer model)
        {
            try
            {
                if (await _context.PoliceOfficers.AnyAsync(p => p.Email == model.Email))
                    return BadRequest(new { message = "Official Email is already registered." });

                if (await _context.PoliceOfficers.AnyAsync(p => p.BadgeID == model.BadgeID))
                    return BadRequest(new { message = "Badge / Service ID is already registered." });

                model.CreatedAt = DateTime.Now;
                _context.PoliceOfficers.Add(model);
                await _context.SaveChangesAsync();

                return Ok(new { status = "Success", message = "Police Officer account registered successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error: " + (ex.InnerException?.Message ?? ex.Message) });
            }
        }

        [HttpPost("UpdatePolice")]
        public async Task<IActionResult> UpdatePolice([FromForm] PoliceOfficer model)
        {
            try
            {
                var existingOfficer = await _context.PoliceOfficers.FindAsync(model.PoliceID);
                if (existingOfficer == null)
                    return NotFound(new { message = "Police officer account not found." });

                existingOfficer.StationName = model.StationName;
                existingOfficer.BadgeID = model.BadgeID;
                existingOfficer.Email = model.Email;
                existingOfficer.PhoneNo = model.PhoneNo;
                existingOfficer.JurisdictionAddress = model.JurisdictionAddress;

                if (!string.IsNullOrEmpty(model.Password) && model.Password != "********")
                {
                    existingOfficer.Password = model.Password;
                }

                await _context.SaveChangesAsync();
                return Ok(new { status = "Success", message = "Police account updated successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Update failed: " + (ex.InnerException?.Message ?? ex.Message) });
            }
        }

        private async Task<string> SaveImage(IFormFile file, string identifier)
        {
            if (file == null || file.Length == 0) return null;

            List<string> allowedExt = new List<string>() { ".jpg", ".jpeg", ".png" };
            var ext = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExt.Contains(ext)) return "Invalid";

            string folder = Path.Combine(_environment.WebRootPath, "Images");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            string myfn = identifier + ext;
            string filePath = Path.Combine(folder, myfn);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return "/Images/" + myfn;
        }

        [HttpGet("GetSkillsByCategory")]
        public async Task<IActionResult> GetSkillsByCategory([FromQuery] int categoryId)
        {
            try
            {
                var skills = await _context.Skills
                    .Where(s => s.CategoryId == categoryId)
                    .Select(s => new
                    {
                        SkillsId = s.SkillsId,
                        SkillName = s.SkillName
                    })
                    .ToListAsync();

                return Ok(skills);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching skills: " + (ex.InnerException?.Message ?? ex.Message) });
            }
        }

        [HttpPost("SubmitWorkerExperience")]
        public async Task<IActionResult> SubmitWorkerExperience([FromBody] Experience model)
        {
            try
            {
                _context.Experiences.Add(model);
                await _context.SaveChangesAsync();

                return Ok(new { status = "Success", message = "Experience added successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error: " + ex.Message });
            }
        }

        [HttpGet("GetCategories")]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var categories = await _context.Categories
                    .Select(c => new
                    {
                        CategoryId = c.CategoryId,
                        CategoryName = c.CategoryName
                    })
                    .ToListAsync();

                return Ok(categories);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching categories: " + (ex.InnerException?.Message ?? ex.Message) });
            }
        }
    }
}