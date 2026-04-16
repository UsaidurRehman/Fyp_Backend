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

                // Use Email to create a unique filename (replacing characters that aren't file-friendly)
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
                return StatusCode(500, new { message = "Error: " + ex.InnerException?.Message ?? ex.Message });
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

                // 1. Update Profile Picture if new one uploaded
                if (model.PictureFile != null)
                {
                    string safeFileName = model.Email.Replace("@", "_").Replace(".", "_");
                    string imagePath = await SaveImage(model.PictureFile, safeFileName);
                    if (imagePath != "Invalid" && imagePath != null)
                    {
                        existingClient.Picture = imagePath;
                    }
                }

                // 2. Update Basic Fields
                existingClient.Name = model.Name;
                existingClient.Phone = model.Phone;
                existingClient.Address = model.Address;
                existingClient.Email = model.Email;

                // 3. Update Password if provided and changed
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

                model.Picture = imagePath;
                model.AvailableStatus = true;

                // 1. Insert Worker first to generate ID
                _context.Workers.Add(model);
                await _context.SaveChangesAsync();

                // 2. Insert Skills/Experiences linked to this Worker ID
                if (!string.IsNullOrEmpty(experiencesJson))
                {
                    var experiences = JsonConvert.DeserializeObject<List<Experience>>(experiencesJson);
                    if (experiences != null)
                    {
                        var uniqueJunctions = new HashSet<(int, int)>();
                        foreach (var exp in experiences)
                        {
                            exp.WorkerId = model.WorkerId;
                            exp.ExperienceId = 0; // Ensure it is treated as new
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
                return StatusCode(500, new { message = ex.Message });
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

                // 1. Update Profile Picture if new one uploaded
                if (model.PictureFile != null)
                {
                    string identifier = model.Cnic ?? existingWorker.Cnic;
                    string imagePath = await SaveImage(model.PictureFile, identifier);
                    if (imagePath != "Invalid" && imagePath != null) 
                    {
                        existingWorker.Picture = imagePath;
                    }
                }

                // 2. Update Basic Fields
                existingWorker.Name = model.Name;
                existingWorker.Phone = model.Phone;
                existingWorker.Address = model.Address;
                existingWorker.Age = model.Age;
                existingWorker.Salary = model.Salary;
                existingWorker.Gender = model.Gender;
                existingWorker.Bio = model.Bio;
                existingWorker.CategoryId = model.CategoryId;

                // 3. Update Password if provided and changed
                if (!string.IsNullOrEmpty(model.Password) && model.Password != "********")
                {
                    existingWorker.Password = model.Password;
                }

                // 4. Update Skills/Experiences (Replace approach for simplicity and consistency)
                if (!string.IsNullOrEmpty(experiencesJson))
                {
                    // Delete existing ones
                    var oldExps = await _context.Experiences.Where(e => e.WorkerId == model.WorkerId).ToListAsync();
                    _context.Experiences.RemoveRange(oldExps);

                    var oldCats = await _context.WorkerCategories.Where(wc => wc.WorkerId == model.WorkerId).ToListAsync();
                    _context.WorkerCategories.RemoveRange(oldCats);

                    await _context.SaveChangesAsync();

                    // Insert fresh ones
                    var experiences = JsonConvert.DeserializeObject<List<Experience>>(experiencesJson);
                    if (experiences != null)
                    {
                        // Deduplicate junctions to avoid tracking errors
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

        // Helper Method using your Naat logic ideas
        private async Task<string> SaveImage(IFormFile file, string identifier)
        {
            if (file == null || file.Length == 0) return null;

            // 1. Validation Logic (from your Naat example)
            List<string> allowedExt = new List<string>() { ".jpg", ".jpeg", ".png" };
            var ext = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExt.Contains(ext)) return "Invalid";

            // 2. Path Logic - use "Images" (capital I) to match existing wwwroot/Images folder
            string folder = Path.Combine(_environment.WebRootPath, "Images");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            // 3. Custom Naming (Identifier + Extension)
            string myfn = identifier + ext;
            string filePath = Path.Combine(folder, myfn);

            // 4. Saving the stream
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return "/Images/" + myfn;
        }
        [HttpGet("GetSkillsByCategory")]
        public async Task<IActionResult> GetSkillsByCategory(int categoryId)
        {
            try
            {
                var skillsList = await _context.Skills
                    .Where(s => s.CategoryId == categoryId && s.SkillName != null && s.SkillName != "")
                    .Select(s => new
                    {
                        id = s.SkillsId,
                        name = s.SkillName
                    })
                    .ToListAsync();

                return Ok(skillsList);
            }
            catch (Exception ex)
            {
                // Log the inner exception if it exists for better debugging
                var errorMsg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return StatusCode(500, new { message = "Database Error: " + errorMsg });
            }
        }
        [HttpPost("SubmitWorkerExperience")]
        public async Task<IActionResult> SubmitWorkerExperience([FromBody] Experience model)
        {
            try
            {
                // Add the experience object directly to the context
                _context.Experiences.Add(model);
                await _context.SaveChangesAsync();

                return Ok(new { status = "Success", message = "Experience added successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error: " + ex.Message });
            }
        }
    }
}