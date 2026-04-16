using Fyp_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Newtonsoft.Json;

namespace Fyp_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Requires valid JWT token
    public class DashboardController : ControllerBase
    {
        private readonly Fyp1Context _context;

        public DashboardController(Fyp1Context context)
        {
            _context = context;
        }

        // For Worker's own dashboard (shows workers linked to a client account)
        [HttpGet("GetWorkers")]
        public async Task<IActionResult> GetWorkers()
        {
            try
            {
                var workers = await _context.Workers
                    .Include(w => w.Category)
                    .Select(w => new
                    {
                        id = w.WorkerId.ToString(),
                        name = w.Name,
                        role = w.Category != null ? w.Category.CategoryName : "Worker",
                        location = w.Address,
                        status = w.AvailableStatus == true ? "Available" : "Booked",
                        type = w.AvailableStatus == true ? "active" : "alert"
                    })
                    .ToListAsync();

                return Ok(workers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching dashboard data." });
            }
        }

        [HttpGet("GetWorkersForClient")]
        public async Task<IActionResult> GetWorkersForClient(
            [FromQuery] List<string>? categories = null,
            [FromQuery] string? search = null,
            [FromQuery] string? gender = null,
            [FromQuery] string? city = null,
            [FromQuery] List<string>? subSkills = null)
        {
            try
            {
                var query = _context.Workers
                    .Include(w => w.Category)
                    .Where(w => w.AvailableStatus == true); // Only show available workers

                // Filter by category names (Matches ANY of the selected categories)
                if (categories != null && categories.Any() && !categories.Contains("All"))
                {
                    query = query.Where(w => w.Category != null && categories.Contains(w.Category.CategoryName));
                }

                // Filter by gender if provided
                if (!string.IsNullOrEmpty(gender) && gender != "Both")
                {
                    query = query.Where(w => w.Gender == gender);
                }

                // Filter by city if provided
                if (!string.IsNullOrEmpty(city))
                {
                    query = query.Where(w => w.Address != null && w.Address.Contains(city));
                }

                // Filter by name if search is provided
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(w => w.Name.Contains(search));
                }

                // AND logic for sub-skills (Worker must possess ALL selected sub-skills)
                if (subSkills != null && subSkills.Any())
                {
                    foreach (var skillName in subSkills)
                    {
                        // Use a fresh Where clause for each skill to force a worker to have matching entries for ALL skillNames
                        // Join with Skills table manually since WorkerCategory doesn't have a navigation property
                        query = query.Where(w => _context.WorkerCategories
                            .Any(wc => wc.WorkerId == w.WorkerId && _context.Skills.Any(s => s.SkillsId == wc.SkillsId && s.SkillName == skillName)));
                    }
                }

                // Materialize the workers with rating and sub-skills
                var workerList = await query.ToListAsync();

                var results = new List<object>();

                foreach (var w in workerList)
                {
                    // Calculate Average Rating
                    var ratings = await _context.Reviews
                        .Where(r => r.Interview != null && r.Interview.WorkerId == w.WorkerId)
                        .Select(r => r.Rating)
                        .ToListAsync();

                    double avgRating = ratings.Any() ? Math.Round(ratings.Average(r => (double)r!), 1) : 0.0;

                    // Get All Category Names (Main Categories)
                    var categoryNames = await _context.WorkerCategories
                        .Where(wc => wc.WorkerId == w.WorkerId)
                        .Join(_context.Categories,
                              wc => wc.CategoryId,
                              c => c.CategoryId,
                              (wc, c) => c.CategoryName)
                        .Where(name => !string.IsNullOrEmpty(name))
                        .Distinct()
                        .ToListAsync();

                    results.Add(new
                    {
                        id = w.WorkerId.ToString(),
                        name = w.Name,
                        role = w.Category != null ? w.Category.CategoryName : "General",
                        city = w.Address ?? "N/A",
                        salary = w.Salary != null ? "Rs." + w.Salary.ToString() : "Not Set",
                        phone = w.Phone,
                        picture = w.Picture,
                        rating = avgRating.ToString("F1"),
                        categories = categoryNames
                    });
                }

                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error: " + ex.Message });
            }
        }

        [HttpGet("GetWorkerDetail/{id}")]
        public async Task<IActionResult> GetWorkerDetail(int id)
        {
            try
            {
                var worker = await _context.Workers
                    .Include(w => w.Category)
                    .Include(w => w.Experiences)
                    .Include(w => w.Interviews)
                        .ThenInclude(i => i.Reviews)
                    .Include(w => w.Interviews)
                        .ThenInclude(i => i.Client)
                    .FirstOrDefaultAsync(w => w.WorkerId == id);

                if (worker == null)
                    return NotFound(new { message = "Worker not found" });

                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
                bool hasActiveInterview = false;
                string activeInterviewStatus = null;
                if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int clientId))
                {
                    var activeInt = worker.Interviews.FirstOrDefault(i =>
                        i.ClientId == clientId &&
                        i.WorkerDecision != "Rejected" &&
                        i.HiringDecision != "Rejected" &&
                        i.Status != "Rejected" &&
                        i.Status != "Completed" && 
                        i.Status != "Terminated"
                    );
                    hasActiveInterview = activeInt != null;
                    activeInterviewStatus = activeInt?.Status;
                }

                // Flatten Reviews and calculate rating
                var allReviews = worker.Interviews
                    .SelectMany(i => i.Reviews.Select(r => new
                    {
                        reviewerName = i.Client?.Name ?? "Anonymous",
                        rating = r.Rating,
                        comment = r.Comment,
                        date = r.ReviewDate?.ToString("MMM dd, yyyy") ?? "N/A"
                    }))
                    .ToList();

                double avgRating = allReviews.Any() ? Math.Round(allReviews.Average(r => (double)(r.rating ?? 0)), 1) : 0.0;

                int pendingRequestCount = worker.Interviews.Count(i => i.WorkerDecision == null || i.WorkerDecision == "Pending");
                int jobNotificationCount = worker.Interviews.Count(i => i.HiringDecision == "Accepted" || i.HiringDecision == "Rejected");
                int terminationCount = worker.Interviews.Count(i => i.Status == "Terminated");

                // 1. FAILSAFE DISCOVERY: Fetch raw junction data first
                var junctionData = await _context.WorkerCategories
                    .Where(wc => wc.WorkerId == worker.WorkerId)
                    .ToListAsync();

                // 2. Fetch lookup data safely (Duplicates handling)
                var categories = await _context.Categories.ToListAsync();
                var categoryLookup = categories
                    .GroupBy(c => c.CategoryId)
                    .ToDictionary(g => g.Key, g => g.First().CategoryName);

                var skills = await _context.Skills.ToListAsync();
                var skillLookup = skills
                    .GroupBy(s => s.SkillsId)
                    .ToDictionary(g => g.Key, g => g.First().SkillName);

                // 3. Process into Primary vs Part-Time based on sequence
                var primarySkills = new List<string>();
                var partTimeSkills = new List<object>();
                string primaryCategoryName = null;
                int? primaryCategoryId = null;

                var partTimeGroups = new Dictionary<string, List<string>>();

                foreach (var item in junctionData)
                {
                    if (primaryCategoryId == null)
                    {
                        primaryCategoryId = item.CategoryId;
                        categoryLookup.TryGetValue(item.CategoryId, out primaryCategoryName);
                    }

                    if (item.CategoryId == primaryCategoryId)
                    {
                        if (skillLookup.TryGetValue(item.SkillsId, out var skillName))
                        {
                            if (!primarySkills.Contains(skillName)) primarySkills.Add(skillName);
                        }
                    }
                    else
                    {
                        if (categoryLookup.TryGetValue(item.CategoryId, out var catName))
                        {
                            if (!partTimeGroups.ContainsKey(catName)) partTimeGroups[catName] = new List<string>();
                            if (skillLookup.TryGetValue(item.SkillsId, out var sName))
                            {
                                if (!partTimeGroups[catName].Contains(sName)) partTimeGroups[catName].Add(sName);
                            }
                        }
                    }
                }

                foreach (var kvp in partTimeGroups)
                {
                    partTimeSkills.Add(new { categoryName = kvp.Key, skills = kvp.Value });
                }

                var result = new
                {
                    id = worker.WorkerId,
                    name = worker.Name,
                    picture = worker.Picture,
                    bio = worker.Bio ?? "Professional service provider committed to excellence and reliability.",
                    role = primaryCategoryName ?? worker.Category?.CategoryName ?? "General Worker",
                    categoryId = primaryCategoryId ?? worker.CategoryId,
                    location = worker.Address ?? "N/A",
                    salary = worker.Salary != null ? worker.Salary.ToString() : "Not Set",
                    gender = worker.Gender ?? "N/A",
                    availability = worker.AvailableStatus == true ? "Available 24/7" : "Currently Booked",
                    rating = avgRating.ToString("F1"),
                    reviewCount = allReviews.Count,
                    pendingRequestCount = pendingRequestCount,
                    jobNotificationCount = jobNotificationCount,
                    terminationCount = terminationCount,
                    hasActiveInterview = hasActiveInterview,
                    activeInterviewStatus = activeInterviewStatus,

                    // Extra fields for editing
                    primarySkills = primarySkills,
                    cnic = worker.Cnic,
                    phone = worker.Phone,
                    age = worker.Age,
                    // Note: Email was removed because it is not currently in the Worker model

                    rawExperiences = worker.Experiences.Select(e => new
                    {
                        CategoryId = e.CategoryId,
                        SkillsId = e.SkillsId,
                        WorkAt = e.WorkAt,
                        Duration = e.Duration,
                        ExpDetail = e.ExpDetail
                    }).ToList(),

                    experiences = worker.Experiences.Select(e => new
                    {
                        title = e.WorkAt ?? "Previous Role",
                        period = e.Duration ?? "N/A",
                        details = e.ExpDetail ?? ""
                    }).ToList(),
                    reviews = allReviews,
                    partTimeSkills = partTimeSkills
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching worker details: " + ex.Message, detail = ex.ToString() });
            }
        }
        [HttpGet("GetWorkerReviews/{workerId}")]
        public async Task<IActionResult> GetWorkerReviews(int workerId)
        {
            try
            {
                var worker = await _context.Workers
                    .Include(w => w.Interviews)
                        .ThenInclude(i => i.Reviews)
                    .Include(w => w.Interviews)
                        .ThenInclude(i => i.Client)
                    .FirstOrDefaultAsync(w => w.WorkerId == workerId);

                if (worker == null)
                    return NotFound(new { message = "Worker not found" });

                var allReviews = worker.Interviews
                    .SelectMany(i => i.Reviews.Select(r => new
                    {
                        id = r.ReviewId.ToString(),
                        name = i.Client?.Name ?? "Anonymous",
                        rating = r.Rating ?? 0,
                        comment = r.Comment ?? "",
                        date = r.ReviewDate?.ToString("MMM dd, yyyy") ?? "N/A",
                        // Mocking duration since it's not in the DB, but can be inferred or left as static
                        duration = "Previous Client"
                    }))
                    .OrderByDescending(r => r.id) // Recent first
                    .ToList();

                double avgRating = allReviews.Any() ? Math.Round(allReviews.Average(r => (double)r.rating), 1) : 0.0;

                return Ok(new
                {
                    averageRating = avgRating,
                    reviewCount = allReviews.Count,
                    reviews = allReviews
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching worker reviews: " + ex.Message });
            }
        }

        [HttpGet("GetFiltersData")]
        public async Task<IActionResult> GetFiltersData()
        {
            try
            {
                var categories = await _context.Categories
                    .Include(c => c.Skills)
                    .Select(c => new
                    {
                        categoryId = c.CategoryId,
                        categoryName = c.CategoryName,
                        skills = c.Skills.Select(s => s.SkillName).ToList()
                    })
                    .ToListAsync();

                return Ok(categories);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching filter data: " + ex.Message });
            }
        }
        [HttpPost("BookInterview")]
        public async Task<IActionResult> BookInterview([FromBody] Interview model)
        {
            try
            {
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

                if (string.IsNullOrEmpty(userIdStr))
                    return Unauthorized(new { message = "Invalid user session." });

                model.ClientId = int.Parse(userIdStr);
                model.Status = "Pending";
                model.HiringDecision = "Pending";

                _context.Interviews.Add(model);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Interview booked successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error booking interview: " + ex.Message });
            }
        }

        [HttpGet("GetClientDashboard")]
        public async Task<IActionResult> GetClientDashboard()
        {
            try
            {
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

                if (string.IsNullOrEmpty(userIdStr))
                    return Unauthorized(new { message = "Invalid user session." });

                int clientId = int.Parse(userIdStr);

                // --- Auto-Finalize Expired Resignations ---
                // If a worker's last working date has passed, automatically move them to history
                var today = DateOnly.FromDateTime(DateTime.Now);
                var expiredResignations = await _context.Resignations
                    .Include(r => r.Interview)
                    .Where(r => r.Interview.ClientId == clientId && 
                               r.Interview.Status == "Finalized" && 
                               r.LastWorkingDate < today)
                    .ToListAsync();

                if (expiredResignations.Any())
                {
                    foreach (var res in expiredResignations)
                    {
                        res.Interview.Status = "Terminated";
                        _context.Terminations.Add(new Termination
                        {
                            InterviewId = res.InterviewId,
                            TerminatedDate = today,
                            TerminatedReason = "Resignation Period Completed: " + (res.ResignationReason ?? "Regular Resignation")
                        });
                    }
                    await _context.SaveChangesAsync();
                }


                // Fetch both active (Finalized) and past (Terminated) workers
                var hiredWorkers = await _context.Interviews
                    .Include(i => i.Worker)
                        .ThenInclude(w => w.Category)
                    .Include(i => i.Resignations)
                    .Include(i => i.Terminations)
                    .Where(i => i.ClientId == clientId && (i.Status == "Finalized" || i.Status == "Terminated"))
                    .Select(i => new
                    {
                        id = i.WorkerId.ToString(),
                        interviewId = i.InterviewId.ToString(),
                        name = i.Worker != null ? i.Worker.Name : "N/A",
                        picture = i.Worker != null ? i.Worker.Picture : null,
                        role = (i.Worker != null && i.Worker.Category != null) ? i.Worker.Category.CategoryName : "Worker",
                        location = i.Address ?? "N/A",
                        dbStatus = i.Status,
                        interviewDate = i.InterviewDate,
                        hasPendingResignation = i.Resignations.Any(),
                        latestTermination = i.Terminations.OrderByDescending(t => t.TerminatedDate).FirstOrDefault()
                    })
                    .ToListAsync();

                var mappedWorkers = hiredWorkers.Select(i => {
                    string finalStatus;
                    string finalType;

                    if (i.dbStatus == "Terminated") {
                        bool wasResignation = i.latestTermination != null && (i.latestTermination.TerminatedReason.Contains("Resignation") || i.hasPendingResignation);
                        finalStatus = wasResignation ? "Resigned" : "Terminated";
                        finalType = wasResignation ? "resigned" : "terminated";
                    } else {
                        if (i.hasPendingResignation) {
                            finalStatus = "On Work";
                            finalType = "alert";
                        } else {
                            finalStatus = "On Work";
                            finalType = "active";
                        }
                    }

                    return new {
                        id = i.id,
                        interviewId = i.interviewId,
                        name = i.name,
                        picture = i.picture,
                        role = i.role,
                        location = i.location,
                        status = finalStatus,
                        type = finalType,
                        date = i.interviewDate != null ? i.interviewDate.Value.ToString("dd MMM yyyy") : "N/A",
                        sortPriority = (finalType == "active" || finalType == "alert") ? 0 : 1,
                        rawDate = i.interviewDate
                    };
                })
                .OrderBy(w => w.sortPriority)
                .ThenByDescending(w => w.rawDate)
                .ToList();

                var pendingCount = await _context.Interviews
                    .CountAsync(i => i.ClientId == clientId && i.Status == "InterviewScheduled" && i.HiringDecision == null);

                return Ok(new
                {
                    hiredWorkers = mappedWorkers,
                    hiredCount = mappedWorkers.Count(w => w.status == "On Work"),
                    pendingInterviewsCount = pendingCount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error: " + ex.Message });
            }
        }

        // ==========================================
        // CLIENT - ACTIVE REQUESTS APIS
        // ==========================================

        [HttpGet("GetActiveRequests/{clientId}")]
        public async Task<IActionResult> GetActiveRequests(int clientId)
        {
            try
            {
                var requests = await _context.Interviews
                    .Include(i => i.Worker)
                        .ThenInclude(w => w.Category)
                    .Where(i => i.ClientId == clientId && i.Status != "Hired" && i.Status != "JobRejected" && i.Status != "Finalized")
                    .Select(i => new
                    {
                        interviewId = i.InterviewId,
                        workerDecision = i.WorkerDecision ?? "Pending",
                        hiringDecision = i.HiringDecision ?? "Pending",
                        workerName = i.Worker != null ? i.Worker.Name : "Unknown",
                        workerImage = i.Worker != null ? i.Worker.Picture : null,
                        workerSkill = i.Worker != null && i.Worker.Category != null ? i.Worker.Category.CategoryName : "Worker",
                        status = i.Status
                    })
                    .ToListAsync();

                return Ok(requests);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching requests: " + ex.Message });
            }
        }

        [HttpPut("UpdateHiringStatus/{interviewId}")]
        public async Task<IActionResult> UpdateHiringStatus(int interviewId, [FromBody] Interview model)
        {
            try
            {
                var interview = await _context.Interviews.FindAsync(interviewId);
                if (interview == null)
                    return NotFound(new { message = "Interview not found." });

                // Update to "Accepted" or "Rejected"
                interview.HiringDecision = model.HiringDecision;

                // Rule 6: when worker accepted our interview in any case if we reject it or Approve it the status will turn to completed
                if (model.HiringDecision == "Accepted" || model.HiringDecision == "Rejected")
                {
                    interview.Status = "Completed";
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = $"Hiring Status {model.HiringDecision} successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating status: " + ex.Message });
            }
        }

        [HttpDelete("DeleteInterviewRequest/{interviewId}")]
        public async Task<IActionResult> DeleteInterviewRequest(int interviewId)
        {
            try
            {
                var interview = await _context.Interviews.FindAsync(interviewId);
                if (interview != null)
                {
                    _context.Interviews.Remove(interview);
                    await _context.SaveChangesAsync();
                }
                return Ok(new { message = "Deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting request: " + ex.Message });
            }
        }

        [HttpGet("GetWorkerRequests")]
        public async Task<IActionResult> GetWorkerRequests()
        {
            try
            {
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

                if (string.IsNullOrEmpty(userIdStr))
                    return Unauthorized(new { message = "Invalid user session." });

                int workerId = int.Parse(userIdStr);

                var pendingRequests = await _context.Interviews
                    .Include(i => i.Client)
                    .Where(i => i.WorkerId == workerId && (i.WorkerDecision == null || i.WorkerDecision == "Pending"))
                    .Select(i => new
                    {
                        id = i.InterviewId.ToString(),
                        client = i.Client != null ? i.Client.Name : "Unknown Client",
                        location = i.Address ?? "N/A",
                        // Send full date/time so frontend can calculate 'time ago' manually or format it
                        timeRaw = i.InterviewDate,
                        time = i.InterviewDate != null ? i.InterviewDate.Value.ToString("MMM dd, hh:mm tt") : "Not Set",
                        service = "Interview Request",
                        clientPhone = i.Client != null ? i.Client.Phone : "N/A",
                        clientPicture = i.Client != null ? i.Client.Picture : null
                    })
                    .ToListAsync();

                return Ok(pendingRequests);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching requests: " + ex.Message });
            }
        }

        [HttpGet("GetAcceptedWorkerRequests")]
        public async Task<IActionResult> GetAcceptedWorkerRequests()
        {
            try
            {
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

                if (string.IsNullOrEmpty(userIdStr))
                    return Unauthorized(new { message = "Invalid user session." });

                int workerId = int.Parse(userIdStr);

                var acceptedRequests = await _context.Interviews
                    .Include(i => i.Client)
                    .Where(i => i.WorkerId == workerId && i.WorkerDecision == "Accepted")
                    .Select(i => new
                    {
                        id = i.InterviewId.ToString(),
                        client = i.Client != null ? i.Client.Name : "Unknown Client",
                        location = i.Address ?? "N/A",
                        timeRaw = i.InterviewDate,
                        time = i.InterviewDate != null ? i.InterviewDate.Value.ToString("MMM dd, hh:mm tt") : "Not Set",
                        service = "Interview Request",
                        clientPhone = i.Client != null ? i.Client.Phone : "N/A",
                        clientPicture = i.Client != null ? i.Client.Picture : null
                    })
                    .ToListAsync();

                return Ok(acceptedRequests);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching accepted requests: " + ex.Message });
            }
        }

        [HttpPut("UpdateWorkerDecision/{id}")]
        public async Task<IActionResult> UpdateWorkerDecision(int id, [FromBody] Interview model)
        {
            try
            {
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

                if (string.IsNullOrEmpty(userIdStr))
                    return Unauthorized(new { message = "Invalid user session." });

                int workerId = int.Parse(userIdStr);

                // Find the interview making sure it belongs to the logged-in worker
                var interview = await _context.Interviews.FirstOrDefaultAsync(i => i.InterviewId == id && i.WorkerId == workerId);

                if (interview == null)
                    return NotFound(new { message = "Interview request not found or unassigned." });

                // e.g., "Accepted" or "Rejected"
                interview.WorkerDecision = model.WorkerDecision;

                // If rejected, usually the overall status drops as well so client knows logic flow is halted
                if (model.WorkerDecision == "Rejected")
                {
                    interview.Status = "Rejected";
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = $"Interview {model.WorkerDecision} successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating decision: " + ex.Message });
            }
        }

        [HttpGet("GetWorkerJobConfirmations")]
        public async Task<IActionResult> GetWorkerJobConfirmations()
        {
            try
            {
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

                if (string.IsNullOrEmpty(userIdStr))
                    return Unauthorized(new { message = "Invalid user session." });

                int workerId = int.Parse(userIdStr);

                var jobs = await _context.Interviews
                    .Include(i => i.Client)
                    .Include(i => i.Worker)
                    .ThenInclude(w => w.Category)
                    .Where(i => i.WorkerId == workerId && (i.HiringDecision == "Accepted" || i.HiringDecision == "Rejected" || i.Status == "Finalized" || i.Status == "Terminated"))
                    .Select(i => new
                    {
                        id = i.InterviewId.ToString(),
                        clientName = i.Client != null ? i.Client.Name : "Client",
                        status = i.Status,
                        date = i.InterviewDate != null ? i.InterviewDate.Value.ToString("dd-MM-yyyy") : "N/A",
                        role = i.Worker != null && i.Worker.Category != null ? i.Worker.Category.CategoryName : "Worker",
                        address = i.Address ?? "N/A",
                        hiringDecision = i.HiringDecision,
                        clientImage = i.Client != null ? i.Client.Picture : null
                    })
                    .ToListAsync();

                var mappedJobs = jobs.Select(item =>
                {
                    string type;
                    string msg;
                    string displayStatus;
                    if (item.hiringDecision == "Rejected")
                    {
                        type = "rejected";
                        msg = "Thank you for your time. After careful consideration, we have decided not to proceed.";
                        displayStatus = "Rejected";
                    }
                    else if (item.status == "Terminated")
                    {
                        type = "terminated";
                        msg = "Your contract has been terminated by the client.";
                        displayStatus = "Terminated";
                    }
                    else
                    { // Accepted by client
                        if (item.status == "Hired")
                        {
                            type = "final"; 
                            msg = "Offer Accepted! Waiting for client to start the contract.";
                            displayStatus = "Accepted";
                        }
                        else if (item.status == "Finalized")
                        {
                            type = "final";
                            msg = "Congratulations! You are officially hired. Welcome aboard!";
                            displayStatus = "Hired";
                        }
                        else
                        {
                            type = "offered";
                            msg = "Great interview! We'd like to proceed with a contract.";
                            displayStatus = "Accepted";
                        }
                    }
                    return new
                    {
                        id = item.id,
                        clientName = item.clientName,
                        status = displayStatus,
                        date = item.date,
                        role = item.role,
                        address = item.address,
                        message = msg,
                        type = type,
                        clientImage = item.clientImage
                    };
                });

                return Ok(mappedJobs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching job confirmations: " + ex.Message });
            }
        }

        [HttpPut("WorkerAcceptJobOffer/{id}")]
        public async Task<IActionResult> WorkerAcceptJobOffer(int id)
        {
            try
            {
                var interview = await _context.Interviews.FindAsync(id);
                if (interview == null) return NotFound(new { message = "Job offer not found." });

                interview.Status = "Hired";
                await _context.SaveChangesAsync();
                return Ok(new { message = "Job Accepted Successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error accepting job: " + ex.Message });
            }
        }

        [HttpPut("WorkerRejectJobOffer/{id}")]
        public async Task<IActionResult> WorkerRejectJobOffer(int id)
        {
            try
            {
                var interview = await _context.Interviews.FindAsync(id);
                if (interview == null) return NotFound(new { message = "Job offer not found." });

                interview.Status = "JobRejected";
                await _context.SaveChangesAsync();
                return Ok(new { message = "Job Rejected Successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error rejecting job: " + ex.Message });
            }
        }

        [HttpGet("GetClientWorkerDecisions")]
        public async Task<IActionResult> GetClientWorkerDecisions()
        {
            try
            {
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

                if (string.IsNullOrEmpty(userIdStr))
                    return Unauthorized(new { message = "Invalid user session." });

                int clientId = int.Parse(userIdStr);

                var decisions = await _context.Interviews
                    .Include(i => i.Worker)
                    .ThenInclude(w => w.Category)
                    .Where(i => i.ClientId == clientId && (i.Status == "Hired" || i.Status == "JobRejected" || i.Status == "Finalized"))
                    .Select(i => new
                    {
                        id = i.InterviewId.ToString(),
                        workerName = i.Worker != null ? i.Worker.Name : "Worker",
                        date = i.InterviewDate != null ? i.InterviewDate.Value.ToString("MMM dd, yyyy") : "N/A",
                        role = i.Worker != null && i.Worker.Category != null ? i.Worker.Category.CategoryName : "Worker",
                        address = i.Address ?? "N/A",
                        status = i.Status,
                        workerImage = i.Worker != null ? i.Worker.Picture : null
                    })
                    .ToListAsync();

                var mappedDecisions = decisions.Select(item => new
                {
                    id = item.id,
                    workerName = item.workerName,
                    date = item.date,
                    role = item.role,
                    address = item.address,
                    status = item.status == "JobRejected" ? "Rejected" : (item.status == "Finalized" ? "Hired" : "Acceptance Confirm"),
                    type = item.status == "JobRejected" ? "rejected" : (item.status == "Finalized" ? "finalized" : "accepted"),
                    message = item.status == "JobRejected" ? $"{item.workerName} has chosen another offer. Your other workers are below." :
                                 (item.status == "Finalized" ? $"{item.workerName} is officially hired. Process completed!" : $"{item.workerName} is excited to start."),
                    workerImage = item.workerImage
                });

                return Ok(mappedDecisions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching worker decisions: " + ex.Message });
            }
        }

        [HttpPut("ClientConfirmWorkerAcceptance/{id}")]
        public async Task<IActionResult> ClientConfirmWorkerAcceptance(int id)
        {
            try
            {
                var interview = await _context.Interviews.FindAsync(id);
                if (interview == null) return NotFound(new { message = "Record not found." });

                interview.Status = "Finalized"; // Mark as permanently finalized so it drops off notifications
                await _context.SaveChangesAsync();
                return Ok(new { message = "Worker acceptance confirmed!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error confirming acceptance: " + ex.Message });
            }
        }

        [HttpDelete("ClientDismissWorkerRejection/{id}")]
        public async Task<IActionResult> ClientDismissWorkerRejection(int id)
        {
            try
            {
                var interview = await _context.Interviews.FindAsync(id);
                if (interview == null) return NotFound(new { message = "Record not found." });

                _context.Interviews.Remove(interview);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Notification dismissed." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error dismissing notification: " + ex.Message });
            }
        }

        [HttpGet("GetActiveJob/{workerId}")]
        public async Task<IActionResult> GetActiveJob(int workerId)
        {
            try
            {
                var activeInterview = await _context.Interviews
                    .Where(i => i.WorkerId == workerId && i.Status == "Finalized" && i.HiringDecision == "Accepted")
                    .Include(i => i.Client)
                    .Select(i => new
                    {
                        interviewId = i.InterviewId,
                        employerName = i.Client != null ? i.Client.Name : "Unknown Employer",
                        employerAddress = i.Client != null ? i.Client.Address : "N/A",
                        hireDate = i.InterviewDate
                    })
                    .FirstOrDefaultAsync();

                if (activeInterview == null)
                    return NotFound(new { message = "No active job found for this worker." });

                return Ok(activeInterview);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching active job: " + ex.Message });
            }
        }

        [HttpPost("SubmitResignation")]
        public async Task<IActionResult> SubmitResignation([FromBody] Resignation model)
        {
            try
            {
                if (model == null || string.IsNullOrEmpty(model.ResignationReason))
                    return BadRequest(new { message = "Resignation reason is required." });

                var interview = await _context.Interviews.FindAsync(model.InterviewId);
                if (interview == null)
                    return NotFound(new { message = "Job record not found." });

                // 1. Prevent duplicate resignations
                var alreadyResigned = await _context.Resignations.AnyAsync(r => r.InterviewId == model.InterviewId);
                if (alreadyResigned)
                    return BadRequest(new { message = "You have already submitted a resignation for this job." });

                // 2. Create clean entity
                var resignation = new Resignation
                {
                    InterviewId = model.InterviewId,
                    ResignationReason = model.ResignationReason,
                    LastWorkingDate = model.LastWorkingDate,
                    SubmittedDate = DateTime.Now
                };

                _context.Resignations.Add(resignation);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Resignation submitted successfully." });
            }
            catch (Exception ex)
            {
                // Concise error for mobile display
                var finalMsg = ex.InnerException?.InnerException?.Message 
                               ?? ex.InnerException?.Message 
                               ?? ex.Message;
                return StatusCode(500, new { message = "DB Error: " + finalMsg });
            }
        }

        [HttpGet("GetClientResignations")]
        public async Task<IActionResult> GetClientResignations()
        {
            try
            {
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                             ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

                if (string.IsNullOrEmpty(userIdStr))
                    return Unauthorized(new { message = "Invalid user session." });

                int clientId = int.Parse(userIdStr);

                var data = await _context.Resignations
                    .Include(r => r.Interview)
                        .ThenInclude(i => i.Worker)
                            .ThenInclude(w => w.Category)
                    .Where(r => r.Interview != null && r.Interview.ClientId == clientId)
                    .OrderByDescending(r => r.SubmittedDate)
                    .ToListAsync();

                var results = data.Select(r => new
                {
                    resignationId = r.ResignationId,
                    workerName = r.Interview?.Worker?.Name ?? "Unknown Worker",
                    workerRole = r.Interview?.Worker?.Category?.CategoryName ?? "Worker",
                    reason = r.ResignationReason,
                    lastWorkingDate = r.LastWorkingDate.ToString("MMM dd, yyyy"),
                    submittedDate = r.SubmittedDate != null ? r.SubmittedDate.Value.ToString("MMM dd, yyyy") : "N/A"
                });

                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching resignations: " + ex.Message });
            }
        }

        [HttpGet("GetResignationDetail/{id}")]
        public async Task<IActionResult> GetResignationDetail(int id)
        {
            try
            {
                var resignation = await _context.Resignations
                    .Include(r => r.Interview)
                        .ThenInclude(i => i.Worker)
                            .ThenInclude(w => w.Category)
                    .FirstOrDefaultAsync(r => r.ResignationId == id);

                if (resignation == null)
                    return NotFound(new { message = "Resignation not found." });

                var worker = resignation.Interview?.Worker;
                
                var submitted = resignation.SubmittedDate ?? DateTime.Now.AddDays(-15);
                var lastDayRaw = resignation.LastWorkingDate;
                var lastDay = lastDayRaw.ToDateTime(TimeOnly.MinValue);
                
                int totalNoticeDays = (lastDay - submitted).Days;
                if (totalNoticeDays <= 0) totalNoticeDays = 30;

                int remainingDays = (lastDay - DateTime.Now).Days;
                if (remainingDays < 0) remainingDays = 0;

                double progress = 1.0 - ((double)remainingDays / totalNoticeDays);
                if (progress > 1) progress = 1;
                if (progress < 0) progress = 0;

                return Ok(new
                {
                    resignationId = resignation.ResignationId,
                    interviewId = resignation.InterviewId,
                    workerName = worker?.Name ?? "Unknown",
                    workerRole = worker?.Category?.CategoryName ?? "Worker",
                    workerAvatar = worker?.Picture,
                    reason = resignation.ResignationReason,
                    lastWorkingDate = lastDayRaw.ToString("MMM dd, yyyy"),
                    totalNoticeDays = totalNoticeDays,
                    remainingDays = remainingDays,
                    progress = Math.Round(progress, 2)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error: " + ex.Message });
            }
        }

        [HttpPost("ConfirmResignation")]
        public async Task<IActionResult> ConfirmResignation([FromBody] Review model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var interview = await _context.Interviews.FindAsync(model.InterviewId);
                if (interview == null) return NotFound(new { message = "Interview record not found." });

                var resignation = await _context.Resignations
                    .Where(r => r.InterviewId == model.InterviewId)
                    .OrderByDescending(r => r.SubmittedDate)
                    .FirstOrDefaultAsync();

                var review = new Review
                {
                    InterviewId = model.InterviewId,
                    Rating = model.Rating,
                    Comment = model.Comment,
                    ReviewDate = DateTime.Now
                };
                _context.Reviews.Add(review);

                interview.Status = "Terminated";

                var termination = new Termination
                {
                    InterviewId = interview.InterviewId,
                    TerminatedDate = DateOnly.FromDateTime(DateTime.Now),
                    TerminatedReason = "Resignation Confirmed" + (resignation != null ? ": " + resignation.ResignationReason : "")
                };
                _context.Terminations.Add(termination);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = "Resignation confirmed and worker record updated." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Error: " + ex.Message });
            }
        }

        [HttpPost("TerminateContract")]
        public async Task<IActionResult> TerminateContract([FromBody] TerminationRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var interview = await _context.Interviews.FindAsync(request.InterviewId);
                if (interview == null) return NotFound(new { message = "Job record not found." });

                // 1. Save Review
                var review = new Review
                {
                    InterviewId = request.InterviewId,
                    Rating = request.Rating,
                    Comment = request.Remarks,
                    ReviewDate = DateTime.Now
                };
                _context.Reviews.Add(review);

                // 2. Update Interview Status
                interview.Status = "Terminated";

                // 3. Create Termination Record
                var termination = new Termination
                {
                    InterviewId = request.InterviewId,
                    TerminatedDate = DateOnly.FromDateTime(DateTime.Now),
                    TerminatedReason = request.Reason
                };
                _context.Terminations.Add(termination);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = "Contract terminated successfully." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Error: " + ex.Message });
            }
        }

        [HttpGet("GetLatestTermination/{workerId}")]
        public async Task<IActionResult> GetLatestTermination(int workerId)
        {
            try
            {
                var termination = await _context.Terminations
                    .Include(t => t.Interview)
                        .ThenInclude(i => i.Client)
                    .Where(t => t.Interview.WorkerId == workerId)
                    .OrderByDescending(t => t.TerminatedDate)
                    .Select(t => new
                    {
                        t.TerminationId,
                        t.TerminatedDate,
                        t.TerminatedReason,
                        ClientName = t.Interview.Client.Name,
                        ClientPicture = t.Interview.Client.Picture,
                        Status = t.Interview.Status
                    })
                    .FirstOrDefaultAsync();

                if (termination == null) return NotFound(new { message = "No termination record found." });

                return Ok(termination);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error: " + ex.Message });
            }
        }
    }

    public class TerminationRequest
    {
        public int InterviewId { get; set; }
        public string Reason { get; set; } = null!;
        public string? Remarks { get; set; }
        public int Rating { get; set; }
    }
}
