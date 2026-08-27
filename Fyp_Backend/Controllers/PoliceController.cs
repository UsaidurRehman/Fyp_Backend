using Fyp_Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Fyp_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PoliceController : ControllerBase
    {
        private readonly Fyp1Context _context;

        public PoliceController(Fyp1Context context)
        {
            _context = context;
        }

        [HttpGet("GetWorkersForVerification")]
        public async Task<IActionResult> GetWorkersForVerification([FromQuery] string searchCnic = "")
        {
            try
            {
                var query = _context.Workers.AsQueryable();

                if (!string.IsNullOrWhiteSpace(searchCnic))
                {
                    query = query.Where(w => w.Cnic != null && w.Cnic.Contains(searchCnic));
                }

                var workersList = await query.ToListAsync();

                // Join with WorkerCategories and Categories table, then aggregate categories per worker
                var result = await (from w in query
                                    join wc in _context.WorkerCategories on w.WorkerId equals wc.WorkerId into wcGroup
                                    from wc in wcGroup.DefaultIfEmpty()
                                    join c in _context.Categories on wc.CategoryId equals c.CategoryId into cGroup
                                    from c in cGroup.DefaultIfEmpty()
                                    select new
                                    {
                                        WorkerId = w.WorkerId,
                                        Name = w.Name ?? "",
                                        CategoryName = c != null ? c.CategoryName : null,
                                        Cnic = w.Cnic ?? "",
                                        Picture = w.Picture
                                    }).ToListAsync();

                // Group by worker ID to combine multiple categories into a single distinct card
                var list = result
                    .GroupBy(x => x.WorkerId)
                    .Select(g => new
                    {
                        Id = g.Key,
                        Name = g.First().Name,
                        Category = string.Join(", ", g.Select(x => x.CategoryName).Where(c => !string.IsNullOrEmpty(c)).Distinct()),
                        Cnic = g.First().Cnic,
                        Picture = g.First().Picture
                    })
                    .Select(w => new
                    {
                        w.Id,
                        w.Name,
                        Category = string.IsNullOrWhiteSpace(w.Category) ? "Domestic Worker" : w.Category,
                        w.Cnic,
                        w.Picture
                    })
                    .ToList();

                return Ok(new
                {
                    totalResults = list.Count,
                    workers = list
                });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = "Server error retrieving workers.", error = ex.Message });
            }
        }
        [HttpGet("GetWorkerDetails/{workerId}")]
        public async Task<IActionResult> GetWorkerDetails(int workerId)
        {
            try
            {
                var worker = await _context.Workers
                    .Where(w => w.WorkerId == workerId)
                    .Select(w => new
                    {
                        Id = w.WorkerId,
                        Name = w.Name ?? "",
                        Cnic = w.Cnic ?? "",
                        Picture = w.Picture
                    })
                    .FirstOrDefaultAsync();

                if (worker == null)
                    return NotFound(new { message = "Worker not found." });

                return Ok(worker);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching worker profile.", error = ex.Message });
            }
        }

        [HttpPost("FileCriminalRecord")]
        public async Task<IActionResult> FileCriminalRecord([FromBody] CriminalRecordDto model)
        {
            if (model == null || model.WorkerId <= 0)
            {
                return BadRequest(new { message = "Invalid payload or Worker ID." });
            }

            try
            {
                // 1. Verify Worker exists in DB
                var workerExists = await _context.Workers.AnyAsync(w => w.WorkerId == model.WorkerId);
                if (!workerExists)
                {
                    return BadRequest(new { message = $"Worker with ID {model.WorkerId} does not exist in the system." });
                }

                // 2. Safely resolve PoliceId foreign key constraint
                int? validPoliceId = null;
                if (model.PoliceId > 0)
                {
                    var policeExists = await _context.PoliceOfficers.AnyAsync(p => p.PoliceID == model.PoliceId);
                    if (policeExists)
                    {
                        validPoliceId = model.PoliceId;
                    }
                }

                // 3. Parse Offense Date safely
                DateTime parsedOffenseDate = DateTime.TryParse(model.OffenseDate, out var tempDate)
                    ? tempDate
                    : DateTime.Now;

                // 4. Construct entity with string length safeguards
                var record = new PoliceRecord
                {
                    WorkerID = model.WorkerId,
                    PoliceID = validPoliceId,
                    // Replace line 169 in PoliceController.cs:
                    FIRNumber = string.IsNullOrWhiteSpace(model.FirNumber) ? "N/A" : model.FirNumber.Trim(),
                    OffenseCategory = model.OffenseCategory?.Length > 150
                        ? model.OffenseCategory.Substring(0, 150)
                        : (model.OffenseCategory ?? "Unspecified"),
                    OffenseDate = parsedOffenseDate,
                    IsFlagged = model.IsFlagged,
                    IsBlocked = model.IsBlocked,
                    CaseDetails = model.CaseDetails ?? string.Empty,
                    FiledDate = DateTime.Now
                };

                _context.PoliceRecords.Add(record);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Record filed successfully." });
            }
            catch (Exception ex)
            {
                // Extract inner exception details to show exact SQL/EF error
                string detailedError = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return StatusCode(500, new { message = "Database insertion failed.", error = detailedError });
            }
        }

        public class CriminalRecordDto
        {
            public int WorkerId { get; set; }
            public int PoliceId { get; set; }
            public string? FirNumber { get; set; }
            public string OffenseCategory { get; set; } = string.Empty;
            public string OffenseDate { get; set; } = string.Empty;
            public bool IsFlagged { get; set; }
            public bool IsBlocked { get; set; }
            public string CaseDetails { get; set; } = string.Empty;
        }
    }
}