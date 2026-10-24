using Fyp_Backend.Models;
using Fyp_Backend.Services;
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

        private int? ActorId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub"), out var id) && id > 0 ? id : null;

        private Task<bool> HasActiveExclusiveFullTimeJobAsync(int workerId, int? excludingInterviewId = null)
            => _context.Hiring.AnyAsync(h => h.HiringDecision == "Accepted" && h.Interview != null
                && h.Interview.WorkerId == workerId
                && h.Interview.JobType == "Full-Time" && h.Interview.IsExclusive
                && h.Interview.Status != "Resigned" && h.Interview.Status != "Terminated"
                && h.Interview.Status != "Completed"
                && (!excludingInterviewId.HasValue || h.InterviewId != excludingInterviewId.Value)
                && !_context.Resignations.Any(r => r.InterviewId == h.InterviewId));

        private Task<bool> HasActiveFullTimeJobAsync(int workerId, int? excludingInterviewId = null)
            => _context.Hiring.AnyAsync(h => h.HiringDecision == "Accepted" && h.Interview != null
                && h.Interview.WorkerId == workerId && h.Interview.JobType == "Full-Time"
                && h.Interview.Status != "Resigned" && h.Interview.Status != "Terminated"
                && h.Interview.Status != "Completed"
                && (!excludingInterviewId.HasValue || h.InterviewId != excludingInterviewId.Value)
                && !_context.Resignations.Any(r => r.InterviewId == h.InterviewId));

        private ObjectResult WorkflowFailure(Exception ex)
        {
            HttpContext?.RequestServices.GetService<ILogger<DashboardController>>()?
                .LogError(ex, "Resignation workflow request failed");
            return StatusCode(500, new { message = "The resignation request could not be completed. Check that the resignation SQL update is applied, then retry." });
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new { message = "Dashboard API is working!" });
        }

        [HttpGet("GetWorkers/{clientId}")]
        public async Task<IActionResult> GetWorkers(int clientId)
        {
            try
            {
                // Query finalized rows out of the Hiring table matching your dashboard schema mappings
                var hiredWorkers = await _context.Hiring
                    .Include(h => h.Interview)
                        .ThenInclude(i => i!.Worker)
                    .Where(h => h.Interview!.ClientId == clientId &&
                                h.WorkerDecision == "Accepted" &&
                                h.HiringDecision == "Accepted")
                    .Select(h => new
                    {
                        // Enforces exact lowercase string attributes demanded by renderWorkerCard destructuring
                        id = h.Interview!.WorkerId.ToString(),
                        name = h.Interview.Worker!.Name,
                        role = _context.Experiences
                            .Where(e => e.WorkerId == h.Interview.WorkerId)
                            .Select(e => e.WorkAt)
                            .FirstOrDefault() ?? "General Assistant",
                        phone = h.Interview.Worker.Phone,
                        salary = h.Interview.Worker.Salary,
                        status = "active",
                        image = h.Interview.Worker.Picture,
                        address = h.Address ?? h.Interview.Address // Fallback safely to interview address
                    })
                    .ToListAsync();

                return Ok(hiredWorkers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to compile hired worker database list: " + ex.Message });
            }
        }
        [HttpGet("GetDashboardStats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int clientId))
                {
                    return BadRequest(new { message = "Invalid token context." });
                }

                // Count pending initial requests (still sitting in Interview table phase)
                int pendingInterviews = await _context.Interviews
                    .CountAsync(i => i.ClientId == clientId && i.Status == "Pending");

                // Count finalized matches that successfully populated the Hiring parameters
                int hiredCount = await _context.Hiring
                    .CountAsync(h => h.Interview!.ClientId == clientId &&
                                     h.WorkerDecision == "Accepted" &&
                                     h.HiringDecision == "Accepted");

                return Ok(new
                {
                    hiredCount = hiredCount,
                    pendingInterviewsCount = pendingInterviews
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error loading metric numbers: " + ex.Message });
            }
        }

        //[HttpGet("GetWorkersForClient")]
        //public async Task<IActionResult> GetWorkersForClient(
        //    [FromQuery] List<string>? categories = null,
        //    [FromQuery] string? search = null,
        //    [FromQuery] string? gender = null,
        //    [FromQuery] string? city = null,
        //    [FromQuery] List<string>? subSkills = null)
        //{
        //    try
        //    {
        //        IQueryable<Worker> query = _context.Workers;

        //        // Filter by category names or IDs (Matches ANY of the selected categories)
        //        if (categories != null && categories.Any() && !categories.Contains("All"))
        //        {
        //            var normalizedCategories = categories
        //                .Where(c => !string.IsNullOrWhiteSpace(c))
        //                .Select(c => c.Trim())
        //                .ToList();

        //            var categoryIds = normalizedCategories
        //                .Where(c => int.TryParse(c, out _))
        //                .Select(int.Parse)
        //                .ToList();

        //            var categoryNames = normalizedCategories
        //                .Where(c => !int.TryParse(c, out _))
        //                .ToList();

        //            query = query.Where(w => _context.WorkerCategories
        //                .Any(wc => wc.WorkerId == w.WorkerId && _context.Categories
        //                    .Any(c => c.CategoryId == wc.CategoryId &&
        //                        (categoryIds.Contains(c.CategoryId) || categoryNames.Contains(c.CategoryName)))));
        //        }

        //        // Filter by gender if provided
        //        if (!string.IsNullOrEmpty(gender) && gender != "Both")
        //        {
        //            query = query.Where(w => w.Gender == gender);
        //        }

        //        // Filter by city if provided
        //        if (!string.IsNullOrEmpty(city))
        //        {
        //            query = query.Where(w => w.Address != null && w.Address.Contains(city));
        //        }

        //        // Filter by name if search is provided
        //        if (!string.IsNullOrEmpty(search))
        //        {
        //            query = query.Where(w => w.Name.Contains(search));
        //        }

        //        // AND logic for sub-skills (Worker must possess ALL selected sub-skills)
        //        if (subSkills != null && subSkills.Any())
        //        {
        //            foreach (var skillName in subSkills)
        //            {
        //                query = query.Where(w => _context.WorkerCategories
        //                    .Any(wc => wc.WorkerId == w.WorkerId && _context.Skills.Any(s => s.SkillsId == wc.SkillsId && s.SkillName == skillName)));
        //            }
        //        }

        //        // Materialize the workers with rating and sub-skills
        //        var workerList = await query.ToListAsync();
        //        var results = new List<object>();

        //        foreach (var w in workerList)
        //        {
        //            // Calculate Average Rating
        //            var ratings = await _context.Reviews
        //                .Where(r => r.Interview != null && r.Interview.WorkerId == w.WorkerId)
        //                .Select(r => r.Rating)
        //                .ToListAsync();

        //            double avgRating = ratings.Any() ? Math.Round(ratings.Average(r => (double)r!), 1) : 0.0;

        //            // Get All Category Names (Main Categories)
        //            var categoryNames = await _context.WorkerCategories
        //                .Where(wc => wc.WorkerId == w.WorkerId)
        //                .Join(_context.Categories,
        //                      wc => wc.CategoryId,
        //                      c => c.CategoryId,
        //                      (wc, c) => c.CategoryName)
        //                .Where(name => !string.IsNullOrEmpty(name))
        //                .Distinct()
        //                .ToListAsync();

        //            results.Add(new
        //            {
        //                id = w.WorkerId.ToString(),
        //                name = w.Name,
        //                role = categoryNames.FirstOrDefault() ?? "General",
        //                city = w.Address ?? "N/A",
        //                salary = w.Salary != null ? "Rs." + w.Salary.ToString() : "Not Set",
        //                phone = w.Phone,
        //                picture = w.Picture,
        //                rating = avgRating.ToString("F1"),
        //                gender = w.Gender ?? "N/A",
        //                categories = categoryNames,
        //                availableStatus = w.AvailableStatus ?? false,
        //            });
        //        }

        //        return Ok(results);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { message = "Error: " + ex.Message });
        //    }
        //}
        [HttpGet("GetWorkersForClient")]
        public async Task<IActionResult> GetWorkersForClient(
    [FromQuery] List<string>? categories = null,
    [FromQuery] string? search = null,
    [FromQuery] string? gender = null,
    [FromQuery] string? city = null,
    [FromQuery] List<string>? subSkills = null,
    [FromQuery] List<int>? habitIds = null)
        {
            try
            {
                IQueryable<Worker> query = _context.Workers;

                // Filter by category names or IDs (Matches ANY of the selected categories)
                if (categories != null && categories.Any() && !categories.Contains("All"))
                {
                    var normalizedCategories = categories
                        .Where(c => !string.IsNullOrWhiteSpace(c))
                        .Select(c => c.Trim())
                        .ToList();

                    var categoryIds = normalizedCategories
                        .Where(c => int.TryParse(c, out _))
                        .Select(int.Parse)
                        .ToList();

                    var categoryNames = normalizedCategories
                        .Where(c => !int.TryParse(c, out _))
                        .ToList();

                    query = query.Where(w => _context.WorkerCategories
                        .Any(wc => wc.WorkerId == w.WorkerId && _context.Categories
                            .Any(c => c.CategoryId == wc.CategoryId &&
                                (categoryIds.Contains(c.CategoryId) || categoryNames.Contains(c.CategoryName)))));
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
                    query = query.Where(w => w.Name != null && w.Name.Contains(search));
                }

                // AND logic for sub-skills (Worker must possess ALL selected sub-skills)
                if (subSkills != null && subSkills.Any())
                {
                    foreach (var skillName in subSkills)
                    {
                        query = query.Where(w => _context.WorkerCategories
                            .Any(wc => wc.WorkerId == w.WorkerId && _context.Skills.Any(s => s.SkillsId == wc.SkillsId && s.SkillName == skillName)));
                    }
                }

                // Habits filter — AND logic like sub-skills: a worker must hold
                // every habit the client ticked.
                if (habitIds != null && habitIds.Any())
                {
                    foreach (var habitId in habitIds.Where(id => id > 0).Distinct())
                    {
                        int wantedHabitId = habitId;
                        query = query.Where(w => _context.WorkerHabits
                            .Any(wh => wh.WorkerId == w.WorkerId && wh.HabitId == wantedHabitId));
                    }
                }

                // Materialize the workers with rating and sub-skills
                var workerList = await query.ToListAsync();
                var results = new List<object>();

                // Company certification flag for the "Certified" badge on the worker
                // cards. One set-based query up front (legacy company certifications
                // UNION non-revoked course certificates) instead of two more queries
                // inside the per-worker loop below.
                var certifiedWorkerIds = (await _context.WorkerCertifications
                        .Select(wc => wc.WorkerID)
                        .Union(_context.CourseCertificates
                            .Where(cc => !cc.IsRevoked)
                            .Select(cc => cc.WorkerID))
                        .ToListAsync())
                    .ToHashSet();

                foreach (var w in workerList)
                {
                    // Calculate Average Rating
                    var ratings = await _context.Reviews
                        .Where(r => r.Interview != null && r.Interview.WorkerId == w.WorkerId)
                        .Select(r => r.Rating)
                        .ToListAsync();

                    double avgRating = ratings.Any() ? Math.Round(ratings.Average(r => (double)r!), 1) : 0.0;

                    // Get All Category Names (Main Categories)
                    var workerCategoryNames = await _context.WorkerCategories
                        .Where(wc => wc.WorkerId == w.WorkerId)
                        .Join(_context.Categories,
                              wc => wc.CategoryId,
                              c => c.CategoryId,
                              (wc, c) => c.CategoryName)
                        .Where(name => !string.IsNullOrEmpty(name))
                        .Distinct()
                        .ToListAsync();

                    // Police character-certificate status: certified = a non-revoked
                    // certificate that has not passed its 5-year expiry.
                    bool isPoliceVerified = await _context.PoliceRecords
                        .AnyAsync(pr => pr.WorkerID == w.WorkerId && !pr.IsRevoked && pr.ExpiryDate > DateTime.Now);

                    results.Add(new
                    {
                        id = w.WorkerId.ToString(),
                        name = w.Name,
                        role = workerCategoryNames.FirstOrDefault() ?? "General",
                        city = w.Address ?? "N/A",
                        salary = w.Salary != null ? "Rs." + w.Salary.ToString() : "Not Set",
                        phone = w.Phone,
                        picture = w.Picture,
                        rating = avgRating.ToString("F1"),
                        gender = w.Gender ?? "N/A",
                        categories = workerCategoryNames,
                        availableStatus = w.AvailableStatus ?? false,
                        isPoliceVerified = isPoliceVerified,
                        isCompanyCertified = certifiedWorkerIds.Contains(w.WorkerId)
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
        public async Task<IActionResult> GetWorkerDetail(int id, [FromQuery] int? clientIdParam = null)
        {
            try
            {
                var worker = await _context.Workers
                    .Include(w => w.Experiences)
                    .Include(w => w.Interviews)
                        .ThenInclude(i => i.Reviews)
                            .ThenInclude(r => r.ReviewCriteriaRatings)
                                .ThenInclude(rc => rc.Criteria)
                    .Include(w => w.Interviews)
                        .ThenInclude(i => i.Client)
                    .FirstOrDefaultAsync(w => w.WorkerId == id);

                if (worker == null)
                    return NotFound(new { message = "Worker not found" });

                // Extract Client ID from Claims or Query Parameter
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

                int clientId = 0;
                if (!string.IsNullOrEmpty(userIdStr))
                {
                    int.TryParse(userIdStr, out clientId);
                }
                if (clientId == 0 && clientIdParam.HasValue)
                {
                    clientId = clientIdParam.Value;
                }

                bool hasActiveInterview = false;
                string? activeInterviewStatus = null;
                string? activeJobType = null;  // job type of the running contract with this client
                var client = await _context.Clients.FirstOrDefaultAsync(c => c.ClientId == clientId);

                if (client != null)
                {
                    var activeInt = worker.Interviews.FirstOrDefault(i =>
                        i.ClientId == client.ClientId &&
                        i.WorkerDecision != "Rejected" &&
                        i.Status != "Rejected" &&
                        i.Status != "Completed" &&
                        i.Status != "Terminated" &&
                        // Part-time: a finalized/active part-time contract does not block
                        // the same client from booking extra slots. In-flight requests
                        // (Pending / Approved / worker-accepted-but-not-hired) still block.
                        !(i.JobType == "Part-Time" && (i.Status == "Finalized" || i.Status == "Hired"))
                    );
                    hasActiveInterview = activeInt != null;
                    activeInterviewStatus = activeInt?.Status;
                    activeJobType = activeInt != null ? (activeInt.JobType ?? "Full-Time") : null;
                }

                // --- HAVERSINE RADIUS DISTANCE CALCULATION ---
                bool isWithinRadius = false;
                double distanceKm = 0;
                double workerRadius = worker.Radius > 0 ? worker.Radius : 5.0; // default 5 km

                if (client != null &&
                    worker.Latitude.HasValue && worker.Longitude.HasValue &&
                    client.Latitude.HasValue && client.Longitude.HasValue)
                {
                    double workerLat = Convert.ToDouble(worker.Latitude.Value);
                    double workerLng = Convert.ToDouble(worker.Longitude.Value);
                    double clientLat = Convert.ToDouble(client.Latitude.Value);
                    double clientLng = Convert.ToDouble(client.Longitude.Value);

                    distanceKm = CalculateHaversineDistance(clientLat, clientLng, workerLat, workerLng);

                    if (distanceKm <= workerRadius)
                    {
                        isWithinRadius = true;
                    }
                }

                // --- TIME SLOTS FETCHING (IF WITHIN RADIUS) ---
                var timeSlots = new List<object>();
                if (isWithinRadius)
                {
                    var rawTimeSlots = await _context.WorkerTimeSlots
                        .Where(ts => ts.WorkerId == worker.WorkerId)
                        .ToListAsync();

                    timeSlots = rawTimeSlots.Select(ts => new
                    {
                        id = ts.Id,
                        startTime = ts.StartTime.ToString(@"hh\:mm"),
                        endTime = ts.EndTime.ToString(@"hh\:mm")
                    }).ToList<object>();
                }

                // Reviews processing
                var allReviews = worker.Interviews
                    .SelectMany(i => i.Reviews
                        .Where(r => r.ReviewerRole == "Client")
                        .Select(r => new
                        {
                            interviewId = i.InterviewId,
                            clientId = i.ClientId,
                            reviewerName = i.Client?.Name ?? "Anonymous",
                            // How this contract ended -> drives the Terminated/Resigned badge.
                            endType = i.Status,
                            rating = r.Rating,
                            comment = r.Comment,
                            date = r.ReviewDate?.ToString("MMM dd, yyyy") ?? "N/A",
                            reviewDateRaw = r.ReviewDate,
                            criteria = r.ReviewCriteriaRatings
                                .OrderBy(x => x.Criteria.SortOrder)
                                .ThenBy(x => x.CriteriaId)
                                .Select(x => new { name = x.Criteria.CriteriaName, score = x.Score })
                                .ToList()
                        }))
                    .ToList();

                // Attach the worked period (Hiring_Date -> Termination / Resignation date).
                var detailReviewWindows = await LoadContractWindowsAsync(
                    allReviews.Select(r => r.interviewId).Distinct().ToList(),
                    allReviews.GroupBy(r => r.interviewId).ToDictionary(g => g.Key, g => g.Max(r => r.reviewDateRaw)));

                var allReviewsPayload = allReviews.Select(r =>
                {
                    detailReviewWindows.TryGetValue(r.interviewId, out var window);
                    return new
                    {
                        r.clientId,
                        r.reviewerName,
                        r.endType,
                        endReasonType = window.EndReasonType,
                        endReason = window.EndReason,
                        r.rating,
                        r.comment,
                        r.criteria,
                        r.date,
                        workedFrom = window.From?.ToString("yyyy-MM-dd"),
                        workedTo = window.To?.ToString("yyyy-MM-dd"),
                        workedPeriod = FormatWorkedPeriod(window.From, window.To)
                    };
                }).ToList();

                double avgRating = allReviewsPayload.Any() ? Math.Round(allReviewsPayload.Average(r => (double)(r.rating ?? 0)), 1) : 0.0;

                // Habits the worker selected at signup (or later). Drives the
                // "Habits" tab on their profile and the pre-ticked checkbox list in
                // both the edit-profile form and the My Habits screen.
                var workerHabits = await _context.WorkerHabits
                    .Where(wh => wh.WorkerId == worker.WorkerId && wh.Habit != null && wh.Habit.IsActive)
                    .OrderBy(wh => wh.Habit!.SortOrder)
                    .ThenBy(wh => wh.Habit!.Name)
                    .Select(wh => new { id = wh.Habit!.HabitId, name = wh.Habit!.Name })
                    .ToListAsync();

                // Fetch junction skills
                var junctionData = await _context.WorkerCategories
                    .Where(wc => wc.WorkerId == worker.WorkerId)
                    .ToListAsync();

                var categories = await _context.Categories.ToListAsync();
                var categoryLookup = categories
                    .GroupBy(c => c.CategoryId)
                    .ToDictionary(g => g.Key, g => g.First().CategoryName);

                var skills = await _context.Skills.ToListAsync();
                var skillLookup = skills
                    .GroupBy(s => s.SkillsId)
                    .ToDictionary(g => g.Key, g => g.First().SkillName);

                var primarySkills = new List<string>();
                var partTimeSkills = new List<object>();
                string? primaryCategoryName = null;
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
                    role = primaryCategoryName ?? "General Worker",
                    categoryId = primaryCategoryId,
                    location = worker.Address ?? "N/A",
                    salary = worker.Salary != null ? worker.Salary.ToString() : "Not Set",
                    gender = worker.Gender ?? "N/A",
                    availability = worker.AvailableStatus == true ? "Available 24/7" : "NOT AVAILABLE",
                    availableStatus = worker.AvailableStatus ?? false,
                    rating = avgRating.ToString("F1"),
                    reviewCount = allReviews.Count,
                    hasActiveInterview = hasActiveInterview,
                    activeInterviewStatus = activeInterviewStatus,

                    // Distance & Part Time Radius Info
                    isPartTimeAvailable = isWithinRadius,
                    distanceKm = Math.Round(distanceKm, 2),
                    radius = workerRadius,
                    // Type of the contract currently running with this client, if any
                    activeJobType = activeJobType,
                    timeSlots = timeSlots,

                    primarySkills = primarySkills,
                    cnic = worker.Cnic,
                    phone = worker.Phone,
                    age = worker.Age,

                    rawExperiences = worker.Experiences.Select(e => new
                    {
                        ExperienceId = e.ExperienceId,
                        WorkAt = e.WorkAt,
                        Duration = e.Duration,
                        ExpDetail = e.ExpDetail
                    }).ToList(),

                    rawWorkerCategories = junctionData.Select(wc => new
                    {
                        CategoryId = wc.CategoryId,
                        SkillsId = wc.SkillsId
                    }).ToList(),

                    experiences = worker.Experiences.Select(e => new
                    {
                        title = e.WorkAt ?? "Previous Role",
                        period = e.Duration ?? "N/A",
                        details = e.ExpDetail ?? ""
                    }).ToList(),
                    reviews = allReviewsPayload,
                    habits = workerHabits,
                    partTimeSkills = partTimeSkills
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching worker details: " + ex.Message, detail = ex.ToString() });
            }
        }
        // ═══════════════════════════════════════════════════════════════════════════
        //  GetWorkerAvailableSlots/{workerId}?date=yyyy-MM-dd
        //  Used by InterviewSelectionScreen (part-time bookings) to show the
        //  worker's time slots for the chosen day, marking the ones already taken.
        //  Also returns the authoritative Part-Time / Full-Time verdict for this
        //  client+worker pair, so the app never has to guess.
        // ═══════════════════════════════════════════════════════════════════════════
        [HttpGet("GetWorkerAvailableSlots/{workerId}")]
        public async Task<IActionResult> GetWorkerAvailableSlots(int workerId, [FromQuery] string? date = null)
        {
            try
            {
                var worker = await _context.Workers.FindAsync(workerId);
                if (worker == null)
                    return NotFound(new { message = "Worker not found." });

                // --- who is asking (JWT first, query fallback) ---
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

                int clientId = 0;
                if (!string.IsNullOrEmpty(userIdStr))
                {
                    int.TryParse(userIdStr, out clientId);
                }

                var client = clientId > 0 ? await _context.Clients.FindAsync(clientId) : null;

                // --- the same verdict BookInterview will freeze later ---
                bool isPartTime = ClassifyJobType(client, worker) == "Part-Time";

                double workerRadius = worker.Radius > 0 ? worker.Radius : 5.0;
                double distanceKm = 0;

                if (client != null &&
                    worker.Latitude.HasValue && worker.Longitude.HasValue &&
                    client.Latitude.HasValue && client.Longitude.HasValue)
                {
                    distanceKm = CalculateHaversineDistance(
                        Convert.ToDouble(client.Latitude.Value), Convert.ToDouble(client.Longitude.Value),
                        Convert.ToDouble(worker.Latitude.Value), Convert.ToDouble(worker.Longitude.Value));
                }

                // --- which day are we looking at ---
                DateTime day = DateTime.Today;
                if (!string.IsNullOrWhiteSpace(date) && DateTime.TryParse(date, out var parsed))
                {
                    day = parsed.Date;
                }

                var dayStart = day;
                var dayEnd = day.AddDays(1);

                // --- the worker's recurring daily slots ---
                var rawSlots = await _context.WorkerTimeSlots
                    .Where(s => s.WorkerId == workerId)
                    .OrderBy(s => s.StartTime)
                    .ToListAsync();

                // --- slots already reserved on that day ---
                // A slot is consumed for a given (worker + date). Other dates of the
                // same slot stay open. Dead statuses do not block anything.
                var bookedSlotIds = await _context.Interviews
                    .Where(i => i.WorkerId == workerId &&
                                i.SlotId != null &&
                                i.InterviewDate != null &&
                                i.InterviewDate >= dayStart && i.InterviewDate < dayEnd &&
                                i.Status != "Rejected" &&
                                i.Status != "JobRejected" &&
                                i.Status != "Terminated" &&
                                i.Status != "Resigned" &&
                                i.Status != "Completed")
                    .Select(i => i.SlotId!.Value)
                    .ToListAsync();

                var slots = rawSlots.Select(s => new
                {
                    id = s.Id,
                    startTime = s.StartTime.ToString(@"hh\:mm"),
                    endTime = s.EndTime.ToString(@"hh\:mm"),
                    isTaken = bookedSlotIds.Contains(s.Id)
                });

                return Ok(new
                {
                    workerId = workerId,
                    date = day.ToString("yyyy-MM-dd"),
                    isPartTimeAvailable = isPartTime,
                    radius = workerRadius,
                    distanceKm = Math.Round(distanceKm, 2),
                    hasSlots = rawSlots.Any(),
                    slots = slots
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching worker slots: " + ex.Message });
            }
        }

        //public async Task<IActionResult> GetWorkerDetail(int id, [FromQuery] int? clientIdParam = null)
        //{
        //    try
        //    {
        //        var worker = await _context.Workers
        //            .Include(w => w.Experiences)
        //            .Include(w => w.Interviews)
        //                .ThenInclude(i => i.Reviews)
        //            .Include(w => w.Interviews)
        //                .ThenInclude(i => i.Client)
        //            .FirstOrDefaultAsync(w => w.WorkerId == id);

        //        if (worker == null)
        //            return NotFound(new { message = "Worker not found" });

        //        // Extract Client ID from Claims or Query Parameter
        //        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        //                     ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

        //        int clientId = 0;
        //        if (!string.IsNullOrEmpty(userIdStr))
        //        {
        //            int.TryParse(userIdStr, out clientId);
        //        }
        //        if (clientId == 0 && clientIdParam.HasValue)
        //        {
        //            clientId = clientIdParam.Value;
        //        }

        //        bool hasActiveInterview = false;
        //        string activeInterviewStatus = null;
        //        string? activeJobType = null;
        //        var client = await _context.Clients.FirstOrDefaultAsync(c => c.ClientId == clientId);

        //        if (client != null)
        //        {
        //            var activeInt = worker.Interviews.FirstOrDefault(i =>
        //                i.ClientId == client.ClientId &&
        //                i.WorkerDecision != "Rejected" &&
        //                i.Status != "Rejected" &&
        //                i.Status != "Completed" &&
        //                i.Status != "Terminated" &&
        //                !(i.JobType == "Part-Time" && (i.Status == "Finalized" || i.Status == "Hired"))
        //            );
        //            hasActiveInterview = activeInt != null;
        //            activeInterviewStatus = activeInt?.Status;
        //            activeJobType = activeInt != null ? (activeInt.JobType ?? "Full-Time") : null;
        //        }

        //        // --- HAVERSINE RADIUS DISTANCE CALCULATION ---
        //        bool isWithinRadius = false;
        //        double distanceKm = 0;
        //        double workerRadius = worker.Radius > 0 ? worker.Radius : 5.0;

        //        if (client != null &&
        //            worker.Latitude.HasValue && worker.Longitude.HasValue &&
        //            client.Latitude.HasValue && client.Longitude.HasValue)
        //        {
        //            double workerLat = Convert.ToDouble(worker.Latitude.Value);
        //            double workerLng = Convert.ToDouble(worker.Longitude.Value);
        //            double clientLat = Convert.ToDouble(client.Latitude.Value);
        //            double clientLng = Convert.ToDouble(client.Longitude.Value);

        //            distanceKm = CalculateHaversineDistance(clientLat, clientLng, workerLat, workerLng);

        //            // Fetch radius from worker (Default to 5 km if null or zero)
        //            double workerRadius = worker.Radius > 0 ? worker.Radius : 5.0;

        //            if (distanceKm <= workerRadius)
        //            {
        //                isWithinRadius = true;
        //            }
        //        }

        //        // --- TIME SLOTS FETCHING (IF WITHIN RADIUS) ---
        //        var timeSlots = new List<object>();
        //        if (isWithinRadius)
        //        {
        //            var rawTimeSlots = await _context.WorkerTimeSlots
        //                .Where(ts => ts.WorkerId == worker.WorkerId)
        //                .ToListAsync();

        //            timeSlots = rawTimeSlots.Select(ts => new
        //            {
        //                id = ts.Id,
        //                startTime = ts.StartTime.ToString(@"hh\:mm"),
        //                endTime = ts.EndTime.ToString(@"hh\:mm")
        //            }).ToList<object>();
        //        }

        //        // Reviews processing
        //        var allReviews = worker.Interviews
        //            .SelectMany(i => i.Reviews
        //                .Where(r => r.ReviewerRole == "Client")
        //                .Select(r => new
        //                {
        //                    clientId = i.ClientId,
        //                    reviewerName = i.Client?.Name ?? "Anonymous",
        //                    rating = r.Rating,
        //                    comment = r.Comment,
        //                    date = r.ReviewDate?.ToString("MMM dd, yyyy") ?? "N/A"
        //                }))
        //            .ToList();

        //        double avgRating = allReviews.Any() ? Math.Round(allReviews.Average(r => (double)(r.rating ?? 0)), 1) : 0.0;

        //        int pendingRequestCount = worker.Interviews.Count(i => i.WorkerDecision == null || i.WorkerDecision == "Pending");
        //        int jobNotificationCount = await _context.Hiring.CountAsync(h => h.Interview.WorkerId == worker.WorkerId && h.WorkerDecision == "Pending");
        //        int terminationCount = worker.Interviews.Count(i => i.Status == "Terminated");

        //        // Fetch junction skills
        //        var junctionData = await _context.WorkerCategories
        //            .Where(wc => wc.WorkerId == worker.WorkerId)
        //            .ToListAsync();

        //        var categories = await _context.Categories.ToListAsync();
        //        var categoryLookup = categories
        //            .GroupBy(c => c.CategoryId)
        //            .ToDictionary(g => g.Key, g => g.First().CategoryName);

        //        var skills = await _context.Skills.ToListAsync();
        //        var skillLookup = skills
        //            .GroupBy(s => s.SkillsId)
        //            .ToDictionary(g => g.Key, g => g.First().SkillName);

        //        var primarySkills = new List<string>();
        //        var partTimeSkills = new List<object>();
        //        string primaryCategoryName = null;
        //        int? primaryCategoryId = null;

        //        var partTimeGroups = new Dictionary<string, List<string>>();

        //        foreach (var item in junctionData)
        //        {
        //            if (primaryCategoryId == null)
        //            {
        //                primaryCategoryId = item.CategoryId;
        //                categoryLookup.TryGetValue(item.CategoryId, out primaryCategoryName);
        //            }

        //            if (item.CategoryId == primaryCategoryId)
        //            {
        //                if (skillLookup.TryGetValue(item.SkillsId, out var skillName))
        //                {
        //                    if (!primarySkills.Contains(skillName)) primarySkills.Add(skillName);
        //                }
        //            }
        //            else
        //            {
        //                if (categoryLookup.TryGetValue(item.CategoryId, out var catName))
        //                {
        //                    if (!partTimeGroups.ContainsKey(catName)) partTimeGroups[catName] = new List<string>();
        //                    if (skillLookup.TryGetValue(item.SkillsId, out var sName))
        //                    {
        //                        if (!partTimeGroups[catName].Contains(sName)) partTimeGroups[catName].Add(sName);
        //                    }
        //                }
        //            }
        //        }

        //        foreach (var kvp in partTimeGroups)
        //        {
        //            partTimeSkills.Add(new { categoryName = kvp.Key, skills = kvp.Value });
        //        }

        //        var result = new
        //        {
        //            id = worker.WorkerId,
        //            name = worker.Name,
        //            picture = worker.Picture,
        //            bio = worker.Bio ?? "Professional service provider committed to excellence and reliability.",
        //            role = primaryCategoryName ?? "General Worker",
        //            categoryId = primaryCategoryId,
        //            location = worker.Address ?? "N/A",
        //            salary = worker.Salary != null ? worker.Salary.ToString() : "Not Set",
        //            gender = worker.Gender ?? "N/A",
        //            availability = worker.AvailableStatus == true ? "Available 24/7" : "NOT AVAILABLE",
        //            availableStatus = worker.AvailableStatus ?? false,
        //            rating = avgRating.ToString("F1"),
        //            reviewCount = allReviews.Count,
        //            pendingRequestCount = pendingRequestCount,
        //            jobNotificationCount = jobNotificationCount,
        //            terminationCount = terminationCount,
        //            hasActiveInterview = hasActiveInterview,
        //            activeInterviewStatus = activeInterviewStatus,

        //            // Distance & Part Time Radius Info
        //            isPartTimeAvailable = isWithinRadius,
        //            distanceKm = Math.Round(distanceKm, 2),
        //            timeSlots = timeSlots,

        //            primarySkills = primarySkills,
        //            cnic = worker.Cnic,
        //            phone = worker.Phone,
        //            age = worker.Age,

        //            rawExperiences = worker.Experiences.Select(e => new
        //            {
        //                CategoryId = e.CategoryId,
        //                SkillsId = e.SkillsId,
        //                WorkAt = e.WorkAt,
        //                Duration = e.Duration,
        //                ExpDetail = e.ExpDetail
        //            }).ToList(),

        //            experiences = worker.Experiences.Select(e => new
        //            {
        //                title = e.WorkAt ?? "Previous Role",
        //                period = e.Duration ?? "N/A",
        //                details = e.ExpDetail ?? ""
        //            }).ToList(),
        //            reviews = allReviews,
        //            partTimeSkills = partTimeSkills
        //        };

        //        return Ok(result);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { message = "Error fetching worker details: " + ex.Message, detail = ex.ToString() });
        //    }
        //}


        // Helper: Haversine distance formula calculation
        private double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371.0; // Earth radius in kilometers
            double dLat = ToRadians(lat2 - lat1);
            double dLon = ToRadians(lon2 - lon1);

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double val) => (Math.PI / 180.0) * val;

        // ---------- Contract lifetime helpers (the "Worked: ..." line on review cards) ----------

        // "23 May – 22 Jun 2025"  — the year is repeated on both ends only when it differs.
        private static string? FormatWorkedSpan(DateTime? from, DateTime? to)
        {
            if (from == null || to == null) return null;
            var f = from.Value.Date;
            var t = to.Value.Date;
            if (t < f) t = f;
            // \u2013 is the en dash — escaped so the separator survives any
            // source-file encoding the publish pipeline may assume.
            return f.Year == t.Year
                ? $"{f:dd MMM} \u2013 {t:dd MMM yyyy}"
                : $"{f:dd MMM yyyy} \u2013 {t:dd MMM yyyy}";
        }

        // "23 May – 22 Jun 2025 (30 days)"
        private static string? FormatWorkedPeriod(DateTime? from, DateTime? to)
        {
            var span = FormatWorkedSpan(from, to);
            if (span == null || from == null || to == null) return null;
            var days = (to.Value.Date - from.Value.Date).Days;
            if (days < 0) days = 0;
            return span + (days <= 1 ? " (1 day)" : $" ({days} days)");
        }

        // Batch the lifetime and ending reason for the contracts behind these reviews.
        // Reasons are selected by InterviewId, never by the worker's latest job.
        // Reuse the existing queries so rendering more cards does not add per-review reads.
        private async Task<Dictionary<int, (DateTime? From, DateTime? To, string? EndReasonType, string? EndReason)>> LoadContractWindowsAsync(
            List<int> interviewIds, Dictionary<int, DateTime?> fallbackEndDates)
        {
            var windows = new Dictionary<int, (DateTime? From, DateTime? To, string? EndReasonType, string? EndReason)>();
            if (interviewIds == null || interviewIds.Count == 0) return windows;

            var hireDates = (await _context.Hiring
                .Where(h => h.InterviewId != null && interviewIds.Contains(h.InterviewId.Value))
                .Select(h => new { InterviewId = h.InterviewId!.Value, h.HiringDate })
                .ToListAsync())
                .GroupBy(x => x.InterviewId)
                .ToDictionary(g => g.Key, g => g.Min(x => x.HiringDate));

            var interviews = (await _context.Interviews
                .Where(i => interviewIds.Contains(i.InterviewId))
                .Select(i => new { i.InterviewId, i.InterviewDate, i.Status })
                .ToListAsync())
                .ToDictionary(x => x.InterviewId);

            var resignations = await _context.Resignations
                .Where(r => r.InterviewId != null && interviewIds.Contains(r.InterviewId.Value))
                .Select(r => new
                {
                    InterviewId = r.InterviewId!.Value,
                    r.ResignationId,
                    r.SubmittedDate,
                    r.LastWorkingDate,
                    r.AcceptedAtUtc,
                    r.ResignationReason
                })
                .ToListAsync();

            var terminations = await _context.Terminations
                .Where(t => t.InterviewId != null && interviewIds.Contains(t.InterviewId.Value))
                .Select(t => new
                {
                    InterviewId = t.InterviewId!.Value,
                    t.TerminationId,
                    t.TerminatedDate,
                    t.TerminatedReason
                })
                .ToListAsync();

            foreach (var id in interviewIds.Distinct())
            {
                interviews.TryGetValue(id, out var interview);
                DateTime? from = hireDates.TryGetValue(id, out var hireDate) ? hireDate : null;
                if (from == null) from = interview?.InterviewDate;

                // Deterministic selection also handles legacy rows with null dates.
                var termination = terminations
                    .Where(t => t.InterviewId == id)
                    .OrderByDescending(t => t.TerminatedDate)
                    .ThenByDescending(t => t.TerminationId)
                    .FirstOrDefault();
                var resignation = resignations
                    .Where(r => r.InterviewId == id)
                    .OrderByDescending(r => r.AcceptedAtUtc.HasValue)
                    .ThenByDescending(r => r.AcceptedAtUtc ?? r.SubmittedDate)
                    .ThenByDescending(r => r.ResignationId)
                    .FirstOrDefault();

                DateTime? to = null;
                if (resignation?.AcceptedAtUtc is DateTime acceptedAt &&
                    string.Equals(interview?.Status, "Resigned", StringComparison.OrdinalIgnoreCase))
                {
                    to = ResignationWorkflowService.BusinessDate(acceptedAt).ToDateTime(TimeOnly.MinValue);
                }
                else if (termination?.TerminatedDate is DateOnly terminatedDate)
                {
                    to = terminatedDate.ToDateTime(TimeOnly.MinValue);
                }
                else if (resignation != null)
                {
                    to = resignation.AcceptedAtUtc.HasValue
                        ? ResignationWorkflowService.BusinessDate(resignation.AcceptedAtUtc.Value).ToDateTime(TimeOnly.MinValue)
                        : resignation.LastWorkingDate != default
                            ? resignation.LastWorkingDate.ToDateTime(TimeOnly.MinValue)
                            : resignation.SubmittedDate;
                }

                if (to == null && fallbackEndDates != null && fallbackEndDates.TryGetValue(id, out var reviewDate))
                    to = reviewDate;

                var endReasonType = ResolveReviewEndReasonType(
                    interview?.Status, termination != null, resignation != null);
                // An explicit ending type must never borrow the other model's reason.
                string? endReason = endReasonType == "Terminated"
                    ? termination?.TerminatedReason
                    : endReasonType == "Resigned" ? resignation?.ResignationReason : null;
                endReason = string.IsNullOrWhiteSpace(endReason) ? null : endReason.Trim();

                windows[id] = (from, to, endReasonType, endReason);
            }

            return windows;
        }

        private static string? ResolveReviewEndReasonType(
            string? status, bool hasTermination, bool hasResignation)
        {
            var normalizedStatus = status?.Trim();
            if (string.Equals(normalizedStatus, "Terminated", StringComparison.OrdinalIgnoreCase))
                return "Terminated";
            if (string.Equals(normalizedStatus, "Resigned", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(normalizedStatus, "ResignationPending", StringComparison.OrdinalIgnoreCase))
                return "Resigned";

            // Old rows may lack an ending status; use only this contract's records.
            if (hasTermination) return "Terminated";
            if (hasResignation) return "Resigned";
            return null;
        }

        //    [HttpGet("GetWorkerDetail/{id}")]
        //    public async Task<IActionResult> GetWorkerDetail(int id)
        //    {
        //        try
        //        {
        //            var worker = await _context.Workers
        //                .Include(w => w.Experiences)
        //                .Include(w => w.Interviews)
        //                    .ThenInclude(i => i.Reviews)
        //                .Include(w => w.Interviews)
        //                    .ThenInclude(i => i.Client)
        //                .FirstOrDefaultAsync(w => w.WorkerId == id);

        //            if (worker == null)
        //                return NotFound(new { message = "Worker not found" });

        //            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        //                         ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
        //            bool hasActiveInterview = false;
        //            string activeInterviewStatus = null;
        //            if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int clientId))
        //            {
        //                var activeInt = worker.Interviews.FirstOrDefault(i =>
        //                    i.ClientId == clientId &&
        //                    i.WorkerDecision != "Rejected" &&
        //                    i.Status != "Rejected" &&
        //                    i.Status != "Completed" &&
        //                    i.Status != "Terminated"
        //                );
        //                hasActiveInterview = activeInt != null;
        //                activeInterviewStatus = activeInt?.Status;
        //            }

        //            // Flatten Reviews and calculate rating
        //            //var allReviews = worker.Interviews
        //            //    .SelectMany(i => i.Reviews.Select(r => new
        //            //    {
        //            //        reviewerName = i.Client?.Name ?? "Anonymous",
        //            //        rating = r.Rating,
        //            //        comment = r.Comment,
        //            //        date = r.ReviewDate?.ToString("MMM dd, yyyy") ?? "N/A"
        //            //    }))
        //            //    .ToList();
        //            var allReviews = worker.Interviews
        //.SelectMany(i => i.Reviews
        //    .Where(r => r.ReviewerRole == "Client")
        //    .Select(r => new
        //    {
        //        reviewerName = i.Client?.Name ?? "Anonymous",
        //        rating = r.Rating,
        //        comment = r.Comment,
        //        date = r.ReviewDate?.ToString("MMM dd, yyyy") ?? "N/A"
        //    }))
        //.ToList();

        //            double avgRating = allReviews.Any() ? Math.Round(allReviews.Average(r => (double)(r.rating ?? 0)), 1) : 0.0;

        //            int pendingRequestCount = worker.Interviews.Count(i => i.WorkerDecision == null || i.WorkerDecision == "Pending");
        //            int jobNotificationCount = await _context.Hiring.CountAsync(h => h.Interview.WorkerId == worker.WorkerId && h.WorkerDecision == "Pending");
        //            int terminationCount = worker.Interviews.Count(i => i.Status == "Terminated");

        //            // 1. Fetch raw junction data first
        //            var junctionData = await _context.WorkerCategories
        //                .Where(wc => wc.WorkerId == worker.WorkerId)
        //                .ToListAsync();

        //            // 2. Fetch lookup data safely
        //            var categories = await _context.Categories.ToListAsync();
        //            var categoryLookup = categories
        //                .GroupBy(c => c.CategoryId)
        //                .ToDictionary(g => g.Key, g => g.First().CategoryName);

        //            var skills = await _context.Skills.ToListAsync();
        //            var skillLookup = skills
        //                .GroupBy(s => s.SkillsId)
        //                .ToDictionary(g => g.Key, g => g.First().SkillName);

        //            // 3. Process into Primary vs Part-Time based on sequence
        //            var primarySkills = new List<string>();
        //            var partTimeSkills = new List<object>();
        //            string primaryCategoryName = null;
        //            int? primaryCategoryId = null;

        //            var partTimeGroups = new Dictionary<string, List<string>>();

        //            foreach (var item in junctionData)
        //            {
        //                if (primaryCategoryId == null)
        //                {
        //                    primaryCategoryId = item.CategoryId;
        //                    categoryLookup.TryGetValue(item.CategoryId, out primaryCategoryName);
        //                }

        //                if (item.CategoryId == primaryCategoryId)
        //                {
        //                    if (skillLookup.TryGetValue(item.SkillsId, out var skillName))
        //                    {
        //                        if (!primarySkills.Contains(skillName)) primarySkills.Add(skillName);
        //                    }
        //                }
        //                else
        //                {
        //                    if (categoryLookup.TryGetValue(item.CategoryId, out var catName))
        //                    {
        //                        if (!partTimeGroups.ContainsKey(catName)) partTimeGroups[catName] = new List<string>();
        //                        if (skillLookup.TryGetValue(item.SkillsId, out var sName))
        //                        {
        //                            if (!partTimeGroups[catName].Contains(sName)) partTimeGroups[catName].Add(sName);
        //                        }
        //                    }
        //                }
        //            }

        //            foreach (var kvp in partTimeGroups)
        //            {
        //                partTimeSkills.Add(new { categoryName = kvp.Key, skills = kvp.Value });
        //            }

        //            var result = new
        //            {
        //                id = worker.WorkerId,
        //                name = worker.Name,
        //                picture = worker.Picture,
        //                bio = worker.Bio ?? "Professional service provider committed to excellence and reliability.",
        //                role = primaryCategoryName ?? "General Worker",
        //                categoryId = primaryCategoryId, // Assigned from tracking loop logic directly instead of worker.CategoryId
        //                location = worker.Address ?? "N/A",
        //                salary = worker.Salary != null ? worker.Salary.ToString() : "Not Set",
        //                gender = worker.Gender ?? "N/A",
        //                availability = worker.AvailableStatus == true ? "Available 24/7" : "NOT AVAILABLE",
        //                availableStatus = worker.AvailableStatus ?? false,
        //                rating = avgRating.ToString("F1"),
        //                reviewCount = allReviews.Count,
        //                pendingRequestCount = pendingRequestCount,
        //                jobNotificationCount = jobNotificationCount,
        //                terminationCount = terminationCount,
        //                hasActiveInterview = hasActiveInterview,
        //                activeInterviewStatus = activeInterviewStatus,

        //                primarySkills = primarySkills,
        //                cnic = worker.Cnic,
        //                phone = worker.Phone,
        //                age = worker.Age,

        //                rawExperiences = worker.Experiences.Select(e => new
        //                {
        //                    CategoryId = e.CategoryId,
        //                    SkillsId = e.SkillsId,
        //                    WorkAt = e.WorkAt,
        //                    Duration = e.Duration,
        //                    ExpDetail = e.ExpDetail
        //                }).ToList(),

        //                experiences = worker.Experiences.Select(e => new
        //                {
        //                    title = e.WorkAt ?? "Previous Role",
        //                    period = e.Duration ?? "N/A",
        //                    details = e.ExpDetail ?? ""
        //                }).ToList(),
        //                reviews = allReviews,
        //                partTimeSkills = partTimeSkills
        //            };

        //            return Ok(result);
        //        }
        //        catch (Exception ex)
        //        {
        //            return StatusCode(500, new { message = "Error fetching worker details: " + ex.Message, detail = ex.ToString() });
        //        }
        //    }


        [HttpGet("GetWorkerReviews/{workerId}")]
        public async Task<IActionResult> GetWorkerReviews(int workerId)
        {
            try
            {
                var worker = await _context.Workers
                    .Include(w => w.Interviews)
                        .ThenInclude(i => i.Reviews)
                            .ThenInclude(r => r.ReviewCriteriaRatings)
                                .ThenInclude(rc => rc.Criteria)
                    .Include(w => w.Interviews)
                        .ThenInclude(i => i.Client)
                    .FirstOrDefaultAsync(w => w.WorkerId == workerId);

                if (worker == null)
                    return NotFound(new { message = "Worker not found" });

                //var clientReviews = worker.Interviews
                //    .SelectMany(i => i.Reviews
                //        .Where(r => r.ReviewerRole == "Client")
                //        .Select(r => new
                //        {
                //            id = r.ReviewId.ToString(),
                //            clientId = i.ClientId, // Ensure ClientId is cleanly passed here
                //            name = i.Client != null ? i.Client.Name : "Client",
                //            rating = r.Rating ?? 0,
                //            comment = r.Comment ?? "",
                //            date = r.ReviewDate?.ToString("MMM dd, yyyy") ?? "N/A",
                //            duration = "Previous Client"
                //        }))
                //    .OrderByDescending(r => r.id)
                //    .ToList();
                var clientReviews = worker.Interviews
                    .SelectMany(i => i.Reviews
                        .Where(r => r.ReviewerRole == "Client")
                        // Order on the real columns: the old string sort on the projected id put
                        // "9" after "10". Each row now also carries its contract window.
                        .OrderByDescending(r => r.ReviewDate)
                        .ThenByDescending(r => r.ReviewId)
                        .Select(r => new
                        {
                            id = r.ReviewId.ToString(),
                            interviewId = i.InterviewId,
                            clientId = i.ClientId,
                            name = i.Client != null ? i.Client.Name : "Client",
                            endType = i.Status,
                            rating = r.Rating ?? 0,
                            comment = r.Comment ?? "",
                            date = r.ReviewDate?.ToString("MMM dd, yyyy") ?? "N/A",
                            reviewDateRaw = r.ReviewDate,
                            criteria = r.ReviewCriteriaRatings
                                .OrderBy(x => x.Criteria.SortOrder)
                                .ThenBy(x => x.CriteriaId)
                                .Select(x => new { name = x.Criteria.CriteriaName, score = x.Score })
                                .ToList()
                        }))
                    .ToList();

                // Worked period on every review: Hiring_Date -> Termination / Resignation date.
                var workerReviewWindows = await LoadContractWindowsAsync(
                    clientReviews.Select(r => r.interviewId).Distinct().ToList(),
                    clientReviews.GroupBy(r => r.interviewId).ToDictionary(g => g.Key, g => g.Max(r => r.reviewDateRaw)));

                var reviewPayload = clientReviews.Select(r =>
                {
                    workerReviewWindows.TryGetValue(r.interviewId, out var window);
                    return new
                    {
                        r.id,
                        r.clientId,
                        r.name,
                        r.endType,
                        endReasonType = window.EndReasonType,
                        endReason = window.EndReason,
                        r.rating,
                        r.comment,
                        r.criteria,
                        r.date,
                        workedFrom = window.From?.ToString("yyyy-MM-dd"),
                        workedTo = window.To?.ToString("yyyy-MM-dd"),
                        workedPeriod = FormatWorkedPeriod(window.From, window.To),
                        // short span, kept for the worker dashboard's "date · span" chip
                        duration = FormatWorkedSpan(window.From, window.To)
                    };
                }).ToList();

                double avgRating = reviewPayload.Any() ? Math.Round(reviewPayload.Average(r => (double)r.rating), 1) : 0.0;

                return Ok(new
                {
                    averageRating = avgRating,
                    reviewCount = reviewPayload.Count,
                    reviews = reviewPayload
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching worker reviews: " + ex.Message });
            }
        }

        //[HttpGet("GetWorkerReviews/{workerId}")]
        //public async Task<IActionResult> GetWorkerReviews(int workerId)
        //{
        //    try
        //    {
        //        var worker = await _context.Workers
        //            .Include(w => w.Interviews)
        //                .ThenInclude(i => i.Reviews)
        //            .Include(w => w.Interviews)
        //                .ThenInclude(i => i.Client)
        //            .FirstOrDefaultAsync(w => w.WorkerId == workerId);

        //        if (worker == null)
        //            return NotFound(new { message = "Worker not found" });

        //        var allReviews = worker.Interviews
        //            .SelectMany(i => i.Reviews.Select(r => new
        //            {
        //                id = r.ReviewId.ToString(),
        //                name = i.Client?.Name ?? "Anonymous",
        //                rating = r.Rating ?? 0,
        //                comment = r.Comment ?? "",
        //                date = r.ReviewDate?.ToString("MMM dd, yyyy") ?? "N/A",
        //                duration = "Previous Client"
        //            }))
        //            .OrderByDescending(r => r.id)
        //            .ToList();

        //        double avgRating = allReviews.Any() ? Math.Round(allReviews.Average(r => (double)r.rating), 1) : 0.0;

        //        return Ok(new
        //        {
        //            averageRating = avgRating,
        //            reviewCount = allReviews.Count,
        //            reviews = allReviews
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { message = "Error fetching worker reviews: " + ex.Message });
        //    }
        //}

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

        public sealed class BookInterviewRequest
        {
            public int? WorkerId { get; set; }
            public DateTime? InterviewDate { get; set; }
            public string? Address { get; set; }
            public int? SlotId { get; set; }
            public bool? IsResidenceProvided { get; set; }
            [System.Text.Json.Serialization.JsonPropertyName("IsExclusive")]
            public bool IsExclusive { get; set; }
        }

        [HttpPost("BookInterview")]
        public async Task<IActionResult> BookInterview([FromBody] BookInterviewRequest request)
        {
            try
            {
                // Bind only fields accepted from the app; never bind the EF entity directly.
                var model = new Interview
                {
                    WorkerId = request.WorkerId,
                    InterviewDate = request.InterviewDate,
                    Address = request.Address,
                    SlotId = request.SlotId,
                    IsResidenceProvided = request.IsResidenceProvided,
                    IsExclusive = request.IsExclusive
                };

                // ─── 1. The client always comes from the JWT, never from the body ──
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

                // TryParse instead of Parse: a malformed claim must not turn into a 500
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int clientId))
                    return Unauthorized(new { message = "Invalid user session." });

                model.ClientId = clientId;

                // ─── 2. Load both sides once — they are needed for the job type ────
                var bookingClient = await _context.Clients.FindAsync(clientId);
                if (bookingClient == null)
                    return BadRequest(new { message = "Client profile not found." });

                if (model.WorkerId == null)
                    return BadRequest(new { message = "Worker is required." });

                var bookingWorker = await _context.Workers.FindAsync(model.WorkerId.Value);
                if (bookingWorker == null)
                    return BadRequest(new { message = "Worker not found." });

                // ─── 3. Stop duplicate in-flight requests to the same worker ────────
                // Optional guard: delete this block if you want one client to be able
                // to send several parallel pending requests to the same worker.
                bool alreadyPending = await _context.Interviews.AnyAsync(i =>
                    i.ClientId == clientId &&
                    i.WorkerId == model.WorkerId &&
                    (i.Status == "Pending" || i.Status == "Approved"));

                if (alreadyPending)
                    return BadRequest(new { message = "You already have an active request with this worker." });

                // ─── 4. Rule 1a: both status and workerDecision are pending ─────────
                model.Status = "Pending";
                model.WorkerDecision = "Pending";

                // ─── 5. JOB TYPE (PART-TIME / FULL-TIME) — decided by the server ────
                // Part-Time  = client is inside the worker's own radius (Worker.Radius)
                // Full-Time  = outside it, or the distance cannot be computed
                //              (missing coordinates -> Full-Time, the safe default)
                // Stored once here and never recalculated, so the whole interview +
                // hiring flow keeps showing the same type on both sides.
                // Any JobType the app might send is deliberately overwritten here.
                model.JobType = ClassifyJobType(bookingClient, bookingWorker);

                model.IsExclusive = model.JobType == "Full-Time" && model.IsExclusive;
                if (model.IsExclusive && await HasActiveFullTimeJobAsync(bookingWorker.WorkerId))
                    return Conflict(new { code = "ACTIVE_FULL_TIME_JOB", message = "This worker already has an active full-time job. Proceed without exclusivity or choose another worker." });

                // ─── 5b. PART-TIME ONLY: the client must pick one of the worker's ───
                //         published time slots, and that slot must still be free
                //         for the chosen day. Full-time bookings never reach this
                //         block and keep SlotId = NULL (behaviour unchanged).
                if (model.JobType == "Part-Time")
                {
                    model.IsResidenceProvided = false;
                    if (model.InterviewDate == null)
                        return BadRequest(new { message = "Interview date is required." });

                    if (model.SlotId == null)
                        return BadRequest(new { message = "Please select one of the worker's time slots." });

                    var chosenSlot = await _context.WorkerTimeSlots.FindAsync(model.SlotId.Value);
                    if (chosenSlot == null || chosenSlot.WorkerId != bookingWorker.WorkerId)
                        return BadRequest(new { message = "That time slot does not belong to this worker." });

                    var dayStart = model.InterviewDate.Value.Date;
                    var dayEnd = dayStart.AddDays(1);

                    bool slotTaken = await _context.Interviews.AnyAsync(i =>
                        i.WorkerId == bookingWorker.WorkerId &&
                        i.SlotId == model.SlotId &&
                        i.InterviewDate != null &&
                        i.InterviewDate >= dayStart && i.InterviewDate < dayEnd &&
                        i.Status != "Rejected" &&
                        i.Status != "JobRejected" &&
                        i.Status != "Terminated" &&
                        i.Status != "Resigned" &&
                        i.Status != "Completed");

                    if (slotTaken)
                        return BadRequest(new { message = "That time slot is already booked for this date. Please pick another one." });
                }
                else
                {
                    model.SlotId = null;
                    model.IsResidenceProvided = model.IsResidenceProvided ?? false;
                }
                // ────────────────────────────────────────────────────────────────────

                // ─── 6. Address fallback so the worker's card never shows "N/A" ─────
                if (string.IsNullOrWhiteSpace(model.Address))
                    model.Address = bookingClient.Address;

                _context.Interviews.Add(model);
                await _context.SaveChangesAsync();

                // ─── 5c. Race guard for the picked slot ──────────────────────────────
                // Two clients can tap the same slot within the same second: both pass
                // the check above, both insert. The row inserted LAST is rolled back, so
                // exactly one booking wins the slot and the other client gets the same
                // "already booked" message. Only affects part-time rows with a slot.
                if (model.JobType == "Part-Time" && model.SlotId != null && model.InterviewDate != null)
                {
                    var dayFrom = model.InterviewDate.Value.Date;
                    var dayTo = dayFrom.AddDays(1);

                    bool lostTheRace = await _context.Interviews.AnyAsync(i =>
                        i.InterviewId != model.InterviewId &&
                        i.InterviewId < model.InterviewId &&
                        i.WorkerId == model.WorkerId &&
                        i.SlotId == model.SlotId &&
                        i.InterviewDate != null &&
                        i.InterviewDate >= dayFrom && i.InterviewDate < dayTo &&
                        i.Status != "Rejected" &&
                        i.Status != "JobRejected" &&
                        i.Status != "Terminated" &&
                        i.Status != "Resigned" &&
                        i.Status != "Completed");

                    if (lostTheRace)
                    {
                        _context.Interviews.Remove(model);
                        await _context.SaveChangesAsync();
                        return BadRequest(new { message = "That time slot was just booked by another client. Please pick another one." });
                    }
                }

                // Echo the created id + the frozen type back to the app
                return Ok(new
                {
                    message = $"{model.JobType} interview request booked successfully!",
                    interviewId = model.InterviewId,
                    jobType = model.JobType,
                    isExclusive = model.IsExclusive,
                    slotId = model.SlotId,
                    status = model.Status,
                    workerDecision = model.WorkerDecision
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error booking interview: " + ex.Message });
            }
        }

        // ── helper used above (keep it next to CalculateHaversineDistance) ──
        /// <summary>
        /// Decides whether a booking is Part-Time or Full-Time.
        /// Part-Time means the client is inside the worker's saved radius.
        /// Anything we cannot prove (missing coordinates, no worker) is Full-Time.
        /// </summary>
        private string ClassifyJobType(Client? client, Worker? worker)
        {
            if (client?.Latitude == null || client?.Longitude == null ||
                worker?.Latitude == null || worker?.Longitude == null)
            {
                return "Full-Time";
            }

            double distanceKm = CalculateHaversineDistance(
                Convert.ToDouble(client.Latitude.Value), Convert.ToDouble(client.Longitude.Value),
                Convert.ToDouble(worker.Latitude.Value), Convert.ToDouble(worker.Longitude.Value));

            double workerRadius = worker.Radius > 0 ? worker.Radius : 5.0;

            return distanceKm <= workerRadius ? "Part-Time" : "Full-Time";
        }
        //public async Task<IActionResult> BookInterview([FromBody] Interview model)
        //{
        //    try
        //    {
        //        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        //                     ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

        //        if (string.IsNullOrEmpty(userIdStr))
        //            return Unauthorized(new { message = "Invalid user session." });

        //        model.ClientId = int.Parse(userIdStr);

        //        // Rule 1a: when user books interview, both status and workerDecision should be pending
        //        model.Status = "Pending";
        //        model.WorkerDecision = "Pending";

        //        _context.Interviews.Add(model);
        //        await _context.SaveChangesAsync();

        //        return Ok(new { message = "Interview booked successfully!" });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { message = "Error booking interview: " + ex.Message });
        //    }
        //}

        [HttpGet("GetClientDashboard")]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> GetClientDashboard([FromServices] ResignationWorkflowService workflow, CancellationToken ct = default)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int clientId))
                {
                    return BadRequest(new { message = "Invalid user session token context." });
                }

                await workflow.ProcessDueAsync(clientId: clientId, ct: ct);

                // 1. Calculate active pending initial interview inquiries
                int pendingCount = await _context.Interviews
                    .CountAsync(i => i.ClientId == clientId && i.Status == "Pending");

                // Dots indicate outstanding client work, not unseen/read history.
                // Offers still waiting for a worker and finalized/closed offers do not count.
                int pendingJobRequestsCount = await _context.Hiring.CountAsync(h =>
                    h.Interview != null && h.Interview.ClientId == clientId &&
                    h.HiringDecision == "Pending" &&
                    (h.WorkerDecision == "Accepted" || h.WorkerDecision == "Rejected") &&
                    h.Interview.Status != "Terminated" && h.Interview.Status != "Resigned" &&
                    h.Interview.Status != "Completed");

                // Pending notices and automatically accepted notices awaiting a
                // real Client review need attention. Terminated contracts do not.
                int pendingResignationsCount = await _context.Resignations.CountAsync(r =>
                    r.Interview != null && r.Interview.ClientId == clientId &&
                    (r.Interview.Status != "Resigned" || r.AcceptanceMode == "Automatic") && r.Interview.Status != "Terminated" &&
                    !_context.Reviews.Any(review => review.InterviewId == r.InterviewId &&
                        review.ReviewerRole == "Client"));

                // 2. Count only workers who are not fully terminated
                int workersCount = await _context.Hiring
                    .CountAsync(h => h.Interview!.ClientId == clientId &&
                                     h.WorkerDecision == "Accepted" &&
                                     h.HiringDecision == "Accepted" &&
                                     h.Interview.Status != "Terminated" && h.Interview.Status != "Resigned" &&
                                     h.Interview.Status != "Completed");

                // 3. Compile backend dashboard structured payload data
                var workersList = await _context.Hiring
                    .Include(h => h.Interview)
                        .ThenInclude(i => i!.Worker)
                    .Where(h => h.Interview!.ClientId == clientId &&
                                h.WorkerDecision == "Accepted" &&
                                h.HiringDecision == "Accepted")
                    .Select(h => new
                    {
                        // Mapping using properties expected by the original UserDashboardScreen.js component
                        id = h.Interview!.WorkerId.ToString(),
                        interviewId = h.InterviewId,
                        name = h.Interview.Worker!.Name,
                        role = _context.Experiences
                            .Where(e => e.WorkerId == h.Interview.WorkerId)
                            .Select(e => e.WorkAt)
                            .FirstOrDefault() ?? "Worker",
                        location = h.Address ?? h.Interview.Address,
                        picture = h.Interview.Worker.Picture,
                        jobType = h.Interview.JobType ?? "Full-Time",
                        isExclusive = h.Interview.IsExclusive && h.Interview.JobType == "Full-Time",
                        slotId = h.Interview.SlotId,
                        slotStartTime = _context.WorkerTimeSlots.Where(s => s.Id == h.Interview.SlotId).Select(s => (TimeSpan?)s.StartTime).FirstOrDefault(),
                        slotEndTime = _context.WorkerTimeSlots.Where(s => s.Id == h.Interview.SlotId).Select(s => (TimeSpan?)s.EndTime).FirstOrDefault(),
                        date = h.HiringDate != null ? h.HiringDate.Value.ToString("yyyy-MM-dd") : "",
                        status = h.Interview!.Status == "ResignationPending" ? "Pending Resignation"
                                : h.Interview!.Status == "Resigned" ? "Resigned"
                                : h.Interview!.Status == "Terminated" ? "Terminated"
                                : h.Interview!.Status == "Completed" ? "Completed"
                                : "On Work",
                        resignationId = _context.Resignations.Where(r => r.InterviewId == h.InterviewId)
                            .OrderByDescending(r => r.SubmittedDate).ThenByDescending(r => r.ResignationId)
                            .Select(r => (int?)r.ResignationId).FirstOrDefault(),
                        type = h.Interview!.Status == "ResignationPending" ? "alert"
                             : h.Interview!.Status == "Resigned" ? "resigned"
                             : h.Interview!.Status == "Terminated" ? "terminated"
                             : h.Interview!.Status == "Completed" ? "completed"
                             : "active"
                    })
                    .ToListAsync();

                return Ok(new
                {
                    hiredCount = workersCount,
                    pendingInterviewsCount = pendingCount,
                    pendingJobRequestsCount,
                    pendingResignationsCount,
                    hiredWorkers = workersList
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Dashboard aggregation internal exception: " + ex.Message });
            }
        }


        [HttpPost("CreateHiring")]
        public async Task<IActionResult> CreateHiring([FromBody] HiringDto model)
        {
            try
            {
                // 1. Find the parent Interview record
                var interview = await _context.Interviews.FindAsync(model.InterviewId);
                if (interview == null)
                {
                    return NotFound(new { message = "Associated interview request record not found." });
                }

                // 2. Enforce the workflow rule: Update interview status to "Approved"
                interview.Status = "Approved";

                // 3. Fallback tracking: If the frontend didn't supply an explicit address, copy it from the interview
                string? finalAddress = string.IsNullOrWhiteSpace(model.Address)
                    ? interview.Address
                    : model.Address;

                // 4. Instantiate a fresh entry in the Hiring table matching your state specifications
                var newHiring = new Hiring
                {
                    InterviewId = model.InterviewId,
                    WorkerDecision = "Pending",   // Rule 3: Must be Pending initially
                    HiringDecision = "Pending",   // Rule 3: Must be Pending initially
                    Address = finalAddress,       // Fixed: Persisting the interview address properly
                    HiringDate = DateTime.Now
                };

                _context.Hiring.Add(newHiring);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    status = "Success",
                    message = "Interview approved successfully. Job offer initialized as pending.",
                    hiringId = newHiring.HiringId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal database transaction exception: " + ex.Message });
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
        [Authorize(Roles = "Worker")]
        public async Task<IActionResult> GetWorkerRequests([FromQuery] int? interviewId = null)
        {
            try
            {
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

                if (string.IsNullOrEmpty(userIdStr))
                    return Unauthorized(new { message = "Invalid user session." });

                if (!int.TryParse(userIdStr, out int workerId) || workerId <= 0) return Unauthorized();
                if (interviewId.HasValue && interviewId.Value <= 0) return BadRequest();

                var pendingRequests = await _context.Interviews
                    .Include(i => i.Client)
                    .Where(i => i.WorkerId == workerId && (interviewId.HasValue
                        ? i.InterviewId == interviewId.Value
                        : i.WorkerDecision == null || i.WorkerDecision == "Pending"))
                    .Select(i => new
                    {
                        id = i.InterviewId.ToString(),
                        client = i.Client != null ? i.Client.Name : "Unknown Client",
                        clientId = i.Client != null ? i.Client.ClientId : (int?)null,
                        workerDecision = i.WorkerDecision ?? "Pending",
                        status = i.Status,
                        canRespond = (i.WorkerDecision == null || i.WorkerDecision == "Pending") &&
                            (i.Status == null || i.Status == "Pending" || i.Status == "Scheduled" || i.Status == "Requested"),
                        location = i.Address ?? "N/A",
                        timeRaw = i.InterviewDate,
                        time = i.InterviewDate != null ? i.InterviewDate.Value.ToString("MMM dd, hh:mm tt") : "Not Set",
                        service = "Interview Request",
                        jobType = i.JobType ?? "Full-Time",
                        isExclusive = i.IsExclusive && i.JobType == "Full-Time",
                        slotId = i.SlotId,
                        slotStartTime = _context.WorkerTimeSlots.Where(s => s.Id == i.SlotId).Select(s => (TimeSpan?)s.StartTime).FirstOrDefault(),
                        slotEndTime = _context.WorkerTimeSlots.Where(s => s.Id == i.SlotId).Select(s => (TimeSpan?)s.EndTime).FirstOrDefault(),
                        clientPhone = i.Client != null ? i.Client.Phone : "N/A",
                        clientPicture = i.Client != null ? i.Client.Picture : null,
                        clientRating = i.Client != null ? (_context.Reviews.Any(r => r.Interview!.ClientId == i.ClientId && r.ReviewerRole == "Worker")
                            ? Math.Round(_context.Reviews.Where(r => r.Interview!.ClientId == i.ClientId && r.ReviewerRole == "Worker").Average(r => (double)r.Rating!), 1)
                            : 0.0) : 0.0
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
                        clientId = i.Client.ClientId,
                        location = i.Address ?? "N/A",
                        timeRaw = i.InterviewDate,
                        time = i.InterviewDate != null ? i.InterviewDate.Value.ToString("MMM dd, hh:mm tt") : "Not Set",
                        service = "Interview Request",
                        jobType = i.JobType ?? "Full-Time",
                        isExclusive = i.IsExclusive && i.JobType == "Full-Time",
                        slotId = i.SlotId,
                        slotStartTime = _context.WorkerTimeSlots.Where(s => s.Id == i.SlotId).Select(s => (TimeSpan?)s.StartTime).FirstOrDefault(),
                        slotEndTime = _context.WorkerTimeSlots.Where(s => s.Id == i.SlotId).Select(s => (TimeSpan?)s.EndTime).FirstOrDefault(),
                        clientPhone = i.Client != null ? i.Client.Phone : "N/A",
                        clientPicture = i.Client != null ? i.Client.Picture : null,
                        clientRating = i.Client != null ? (_context.Reviews.Any(r => r.Interview!.ClientId == i.ClientId && r.ReviewerRole == "Worker")
                            ? Math.Round(_context.Reviews.Where(r => r.Interview!.ClientId == i.ClientId && r.ReviewerRole == "Worker").Average(r => (double)r.Rating!), 1)
                            : 0.0) : 0.0
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
        [Authorize(Roles = "Worker")]
        public async Task<IActionResult> UpdateWorkerDecision(int id, [FromBody] Interview model)
        {
            try
            {
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

                if (string.IsNullOrEmpty(userIdStr))
                    return Unauthorized(new { message = "Invalid user session." });

                int workerId = int.Parse(userIdStr);

                var interview = await _context.Interviews.FirstOrDefaultAsync(i => i.InterviewId == id && i.WorkerId == workerId);
                if (interview == null)
                    return NotFound(new { message = "Interview request not found or unassigned." });

                interview.WorkerDecision = model.WorkerDecision;

                if (model.WorkerDecision == "Accepted")
                {
                    interview.Status = "Pending"; // Rule 1b: status pending, workerDecision accepted
                }
                else if (model.WorkerDecision == "Rejected")
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
        [Authorize(Roles = "Worker")]
        public async Task<IActionResult> GetWorkerJobConfirmations([FromQuery] int? hiringId = null)
        {
            try
            {
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

                if (string.IsNullOrEmpty(userIdStr))
                    return Unauthorized(new { message = "Invalid user session." });

                if (!int.TryParse(userIdStr, out int workerId) || workerId <= 0) return Unauthorized();
                if (hiringId.HasValue && hiringId.Value <= 0) return BadRequest();

                var jobs = await _context.Hiring
                    .Include(h => h.Interview)
                        .ThenInclude(i => i.Client)
                    .Include(h => h.Interview)
                        .ThenInclude(i => i.Worker)
                    .Where(h => h.Interview != null && h.Interview.WorkerId == workerId &&
                        (!hiringId.HasValue || h.HiringId == hiringId.Value))
                    .Select(h => new
                    {
                        id = h.InterviewId.ToString(),
                        hiringId = h.HiringId,
                        interviewId = h.InterviewId,
                        clientName = h.Interview.Client != null ? h.Interview.Client.Name : "Client",
                        clientId = h.Interview.Client != null ? h.Interview.Client.ClientId : (int?)null,
                        status = h.Interview.Status,
                        date = h.HiringDate != null ? h.HiringDate.Value.ToString("dd-MM-yyyy") : "Pending",
                        role = _context.WorkerCategories.Where(wc => wc.WorkerId == h.Interview.WorkerId).Join(_context.Categories, wc => wc.CategoryId, c => c.CategoryId, (wc, c) => c.CategoryName).FirstOrDefault() ?? "Worker",
                        address = h.Address ?? "Pending",
                        hiringDecision = h.HiringDecision ?? "Pending",
                        workerDecision = h.WorkerDecision ?? "Pending",
                        jobType = h.Interview.JobType ?? "Full-Time",
                        isExclusive = h.Interview.IsExclusive && h.Interview.JobType == "Full-Time",
                        slotId = h.Interview.SlotId,
                        slotStartTime = _context.WorkerTimeSlots.Where(s => s.Id == h.Interview.SlotId).Select(s => (TimeSpan?)s.StartTime).FirstOrDefault(),
                        slotEndTime = _context.WorkerTimeSlots.Where(s => s.Id == h.Interview.SlotId).Select(s => (TimeSpan?)s.EndTime).FirstOrDefault(),
                        clientImage = h.Interview.Client != null ? h.Interview.Client.Picture : null,
                        clientRating = h.Interview.Client != null ? (_context.Reviews.Any(r => r.Interview!.ClientId == h.Interview.ClientId && r.ReviewerRole == "Worker")
                            ? Math.Round(_context.Reviews.Where(r => r.Interview!.ClientId == h.Interview.ClientId && r.ReviewerRole == "Worker").Average(r => (double)r.Rating!), 1)
                            : 0.0) : 0.0
                    })
                    .ToListAsync();

                //var mappedJobs = jobs.Select(item =>
                //{
                //    string type;
                //    string msg;
                //    string displayStatus;
                //    if (item.workerDecision == "Rejected")
                //    {
                //        type = "rejected";
                //        msg = "Thank you for your time. Job offer declined.";
                //        displayStatus = "Rejected";
                //    }
                //    else if (item.status == "Terminated")
                //    {
                //        type = "terminated";
                //        msg = "Your contract has been terminated by the client.";
                //        displayStatus = "Terminated";
                //    }
                //    else if (item.hiringDecision == "Accepted")
                //    {
                //        type = "final";
                //        msg = "Congratulations! You are officially hired. Welcome aboard!";
                //        displayStatus = "Hired";
                //    }
                //    else if (item.workerDecision == "Accepted")
                //    {
                //        type = "accepted";
                //        msg = "Job offer accepted. Awaiting client response.";
                //        displayStatus = "Accepted";
                //    }
                //    else
                //    {
                //        type = "offered";
                //        msg = "Great interview! We'd like to proceed with a contract.";
                //        displayStatus = "Pending";
                //    }
                //    return new
                //    {
                //        id = item.id,
                //        clientName = item.clientName,
                //        clientRating = item.clientRating,
                //        status = displayStatus,
                //        date = item.date,
                //        role = item.role,
                //        address = item.address,
                //        message = msg,
                //        type = type,
                //        clientImage = item.clientImage
                //    };
                //});
                var mappedJobs = jobs.Select(item =>
                {
                    string type;
                    string msg;
                    string displayStatus;
                    var workerDecision = item.workerDecision.Trim();
                    var hiringDecision = item.hiringDecision.Trim();
                    var contractStatus = item.status?.Trim();
                    if (string.Equals(workerDecision, "Rejected", StringComparison.OrdinalIgnoreCase))
                    {
                        type = "rejected";
                        msg = "Thank you for your time. Job offer declined.";
                        displayStatus = "Rejected";
                    }
                    else if (string.Equals(hiringDecision, "Rejected", StringComparison.OrdinalIgnoreCase))
                    {
                        type = "rejected";
                        msg = "The client decided not to proceed with this job.";
                        displayStatus = "Rejected";
                    }
                    else if (string.Equals(contractStatus, "Terminated", StringComparison.OrdinalIgnoreCase))
                    {
                        type = "terminated";
                        msg = "Your contract has been terminated by the client.";
                        displayStatus = "Terminated";
                    }
                    else if (string.Equals(hiringDecision, "Accepted", StringComparison.OrdinalIgnoreCase))
                    {
                        type = "final";
                        msg = "Congratulations! You are officially hired. Welcome aboard!";
                        displayStatus = "Hired";
                    }
                    else if (string.Equals(workerDecision, "Accepted", StringComparison.OrdinalIgnoreCase))
                    {
                        type = "accepted";
                        msg = "Job offer accepted. Awaiting client response.";
                        displayStatus = "Accepted";
                    }
                    else
                    {
                        type = "offered";
                        msg = "Great interview! We'd like to proceed with a contract.";
                        displayStatus = "Pending";
                    }
                    return new
                    {
                        id = item.id,
                        hiringId = item.hiringId,
                        interviewId = item.interviewId,
                        clientId = item.clientId, // <-- ADD THIS LINE
                        clientName = item.clientName,
                        clientRating = item.clientRating,
                        status = displayStatus,
                        date = item.date,
                        role = item.role,
                        address = item.address,
                        message = msg,
                        type = type,
                        // These four are carried through from the query above. This endpoint
                        // rebuilds every row here, and that second projection silently dropped
                        // them — which is why the Job Offer card kept showing FULL-TIME and no
                        // slot chip while every other screen was correct.
                        jobType = item.jobType,
                        isExclusive = item.isExclusive,
                        slotId = item.slotId,
                        slotStartTime = item.slotStartTime,
                        slotEndTime = item.slotEndTime,
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
        [Authorize(Roles = "Worker")]
        public async Task<IActionResult> WorkerAcceptJobOffer(int id, [FromQuery] int? hiringId = null)
        {
            try
            {
                var rawId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
                if (!int.TryParse(rawId, out int workerId) || workerId <= 0) return Unauthorized();
                if (id <= 0 || (hiringId.HasValue && hiringId.Value <= 0)) return BadRequest();
                var hiring = await _context.Hiring.Include(h => h.Interview).FirstOrDefaultAsync(h =>
                    h.InterviewId == id && h.Interview != null && h.Interview.WorkerId == workerId &&
                    (!hiringId.HasValue || h.HiringId == hiringId.Value));
                if (hiring == null) return NotFound(new { message = "Hiring context job offer record not found." });

                // Rule 2c: if worker accepted but user not responded, workerDecision will be accepted and rest are pending.
                hiring.WorkerDecision = "Accepted";

                await _context.SaveChangesAsync();
                return Ok(new { message = "Job offer accepted by worker! Awaiting client confirmation." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error accepting job: " + ex.Message });
            }
        }

        [HttpPut("WorkerRejectJobOffer/{id}")]
        [Authorize(Roles = "Worker")]
        public async Task<IActionResult> WorkerRejectJobOffer(int id, [FromQuery] int? hiringId = null)
        {
            try
            {
                var rawId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
                if (!int.TryParse(rawId, out int workerId) || workerId <= 0) return Unauthorized();
                if (id <= 0 || (hiringId.HasValue && hiringId.Value <= 0)) return BadRequest();
                var hiring = await _context.Hiring.Include(h => h.Interview).FirstOrDefaultAsync(h =>
                    h.InterviewId == id && h.Interview != null && h.Interview.WorkerId == workerId &&
                    (!hiringId.HasValue || h.HiringId == hiringId.Value));
                if (hiring == null) return NotFound(new { message = "Hiring context record not found." });

                // Rule 2b: worker rejection shifts the tracking record cleanly
                hiring.WorkerDecision = "Rejected";
                hiring.Interview.Status = "JobRejected";

                await _context.SaveChangesAsync();
                return Ok(new { message = "Job Offer declined successfully." });
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
                // 1. Extract the Client ID from the JWT token claims safely
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int clientId))
                {
                    return BadRequest(new { message = "Invalid or expired client authentication token context." });
                }

                // 2. Query data directly out of the Hiring table joined back up to Interview and Worker profiles
                var hiringDecisions = await _context.Hiring
                    .Include(h => h.Interview)
                        .ThenInclude(i => i!.Worker)
                    .Where(h => h.Interview!.ClientId == clientId)
                    .Select(h => new
                    {
                        HiringId = h.HiringId,
                        InterviewId = h.InterviewId,
                        WorkerId = h.Interview!.WorkerId,
                        WorkerName = h.Interview.Worker!.Name,
                        WorkerSkill = _context.Experiences
                            .Where(e => e.WorkerId == h.Interview.WorkerId)
                            .Select(e => e.WorkAt) // Or your corresponding skill field mapping
                            .FirstOrDefault() ?? "General Assistant",
                        WorkerImage = h.Interview.Worker.Picture,

                        // Track state rules from the Hiring table row now
                        WorkerDecision = h.WorkerDecision, // "Pending", "Accepted", "Rejected"
                        HiringDecision = h.HiringDecision, // "Pending", "Accepted" etc.
                        JobType = h.Interview!.JobType ?? "Full-Time",
                        IsExclusive = h.Interview!.IsExclusive && h.Interview.JobType == "Full-Time",
                        slotId = h.Interview!.SlotId,
                        slotStartTime = _context.WorkerTimeSlots.Where(s => s.Id == h.Interview!.SlotId).Select(s => (TimeSpan?)s.StartTime).FirstOrDefault(),
                        slotEndTime = _context.WorkerTimeSlots.Where(s => s.Id == h.Interview!.SlotId).Select(s => (TimeSpan?)s.EndTime).FirstOrDefault(),
                        Address = h.Address,
                        HiringDate = h.HiringDate
                    })
                    .ToListAsync();

                return Ok(hiringDecisions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch active hiring statuses: " + ex.Message });
            }
        }


        [HttpPut("ClientConfirmWorkerAcceptance/{id}")]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> ClientConfirmWorkerAcceptance(int id)
        {
            try
            {
                var clientId = ActorId;
                if (clientId == null) return Unauthorized();
                var interview = await _context.Interviews.FirstOrDefaultAsync(i => i.InterviewId == id && i.ClientId == clientId);
                if (interview == null) return NotFound(new { message = "Record not found." });

                var hiring = await _context.Hiring.FirstOrDefaultAsync(h => h.InterviewId == id && h.WorkerDecision == "Accepted");
                if (hiring == null) return NotFound(new { message = "No worker-accepted hiring offer was found." });
                await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
                if (interview.JobType == "Full-Time" && interview.WorkerId.HasValue)
                {
                    // Serialize full-time confirmation by locking the worker row before checking active contracts.
                    await _context.Workers.FromSqlInterpolated($"SELECT * FROM [dbo].[Worker] WITH (UPDLOCK, ROWLOCK) WHERE [Worker_ID] = {interview.WorkerId.Value}").FirstOrDefaultAsync();
                    if (await HasActiveFullTimeJobAsync(interview.WorkerId.Value, interview.InterviewId))
                        return Conflict(new { code = "ACTIVE_FULL_TIME_JOB", message = "Worker has another active full-time job. They must leave it before this full-time job can be confirmed." });
                }

                // Client confirmation is the point at which the exclusivity commitment begins.
                interview.Status = "Finalized";
                hiring.HiringDecision = "Accepted";
                hiring.HiringDate = DateTime.Now;
                hiring.Address = interview.Address ?? "Confirmed Fleet Address Location";
                if (interview.JobType == "Full-Time" && interview.IsExclusive && interview.WorkerId.HasValue)
                {
                    var worker = await _context.Workers.FindAsync(interview.WorkerId.Value);
                    if (worker != null) worker.AvailableStatus = false;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok(new { message = "Worker acceptance confirmed and contract finalized completely!" });
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
        [Authorize(Roles = "Worker")]
        public async Task<IActionResult> GetActiveJob(int workerId, [FromServices] ResignationWorkflowService workflow, CancellationToken ct = default)
        {
            if (ActorId is not int authenticatedId) return Unauthorized();
            if (authenticatedId != workerId) return Forbid();
            try
            {
                await workflow.ProcessDueAsync(workerId: workerId, ct: ct);
                var activeInterview = await _context.Interviews
                    .Include(i => i.Client)
                    .Where(i => i.WorkerId == workerId &&
                                i.WorkerDecision != "Rejected" &&
                                i.Status != "Rejected" &&
                                i.Status != "JobRejected" &&
                                i.Status != "Completed" &&
                                i.Status != "Terminated" &&
                                i.Status != "Resigned")
                    .GroupJoin(
                        _context.Hiring.Where(h => h.HiringDecision == "Accepted"),
                        i => i.InterviewId,
                        h => h.InterviewId,
                        (i, hiringGroup) => new { Interview = i, HasAcceptedHiring = hiringGroup.Any() }
                    )
                    .Where(x =>
                        x.HasAcceptedHiring ||
                        x.Interview.Status == "Approved" ||
                        x.Interview.Status == "Finalized" ||
                        x.Interview.Status == "Hired" ||
                        x.Interview.Status == "Accepted")
                    .OrderByDescending(x => x.Interview.InterviewId)
                    .Select(x => new
                    {
                        interviewId = x.Interview.InterviewId,
                        employerName = x.Interview.Client != null ? x.Interview.Client.Name : "Unknown Employer",
                        employerAddress = x.Interview.Client != null ? x.Interview.Client.Address : "N/A",
                        hireDate = x.Interview.InterviewDate,
                        resignationWorkflowVersion = 2,
                        noticeDays = 7,
                        isResignationPending = x.Interview.Status == "ResignationPending",
                        resignationId = _context.Resignations.Where(r => r.InterviewId == x.Interview.InterviewId)
                            .OrderByDescending(r => r.SubmittedDate).ThenByDescending(r => r.ResignationId)
                            .Select(r => (int?)r.ResignationId).FirstOrDefault(),
                        noticeEndsAtUtc = _context.Resignations.Where(r => r.InterviewId == x.Interview.InterviewId)
                            .OrderByDescending(r => r.SubmittedDate).ThenByDescending(r => r.ResignationId)
                            .Select(r => r.NoticeEndsAtUtc).FirstOrDefault()
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

        [HttpGet("GetWorkerEndContractDetails/{workerId}")]
        public async Task<IActionResult> GetWorkerEndContractDetails(int workerId)
        {
            try
            {
                // 1. Fetch the raw interview records safely
                var interview = await _context.Interviews
                    .Include(i => i.Client)
                    .Include(i => i.Worker)
                    .Where(i => i.WorkerId == workerId && (i.Status == "Terminated" || i.Status == "Resigned"))
                    .OrderByDescending(i => i.InterviewId)
                    .FirstOrDefaultAsync();

                if (interview == null)
                    return NotFound(new { message = "No terminated or resigned job records found for this worker." });

                string reason = "No details specified";
                string dateStr = "N/A";

                // 2. Fetch records cleanly and execute formatting entirely IN-MEMORY
                if (interview.Status == "Terminated")
                {
                    var term = await _context.Terminations
                        .Where(t => t.InterviewId == interview.InterviewId)
                        .OrderByDescending(t => t.TerminatedDate)
                        .FirstOrDefaultAsync();

                    if (term != null)
                    {
                        reason = term.TerminatedReason ?? "No reason specified";
                        // Added a safety check to ensure TerminatedDate is valid
                        if (term.TerminatedDate.HasValue)
                        {
                            dateStr = term.TerminatedDate.Value.ToDateTime(TimeOnly.MinValue).ToString("dd-MM-yyyy");
                        }
                    }
                }
                else if (interview.Status == "Resigned")
                {
                    var res = await _context.Resignations
                        .Where(r => r.InterviewId == interview.InterviewId)
                        .OrderByDescending(r => r.SubmittedDate)
                        .FirstOrDefaultAsync();

                    if (res != null)
                    {
                        reason = res.ResignationReason ?? "No reason specified";
                        dateStr = res.SubmittedDate.HasValue ? res.SubmittedDate.Value.ToString("dd-MM-yyyy") : "N/A";
                    }
                }

                // 3. Fetch category skills safely
                var workerSkill = await _context.WorkerCategories
                    .Where(wc => wc.WorkerId == workerId)
                    .Join(_context.Categories, wc => wc.CategoryId, c => c.CategoryId, (wc, c) => c.CategoryName)
                    .FirstOrDefaultAsync() ?? "Worker";

                // 4. Return safely formatted object structures with explicit null fallbacks
                return Ok(new
                {
                    status = interview.Status ?? "N/A",
                    date = dateStr,
                    reason = reason,
                    workerName = interview.Worker != null ? interview.Worker.Name : "Unknown",
                    workerPicture = interview.Worker != null ? interview.Worker.Picture : null,
                    workerPhone = interview.Worker != null ? interview.Worker.Phone : "N/A",
                    workerAddress = interview.Worker != null ? interview.Worker.Address : "N/A",
                    workerSkill = workerSkill,
                    clientName = interview.Client != null ? interview.Client.Name : "Client",
                    clientPicture = interview.Client != null ? interview.Client.Picture : null,
                    clientAddress = interview.Client != null ? interview.Client.Address : "N/A"
                });
            }
            catch (Exception ex)
            {
                // Helpful breakdown detail in case any inner exceptions are hiding strings
                var finalMsg = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { message = "Error: " + finalMsg });
            }
        }
        [HttpPost("SubmitResignation")]
        [Authorize(Roles = "Worker")]
        public async Task<IActionResult> SubmitResignation([FromBody] Resignation model,
            [FromServices] ResignationWorkflowService workflow, CancellationToken ct = default)
        {
            if (ActorId is not int workerId) return Unauthorized();
            try
            {
                var notice = await workflow.SubmitAsync(workerId, model.InterviewId ?? 0, model.ResignationReason, ct);
                return Ok(new
                {
                    message = "Resignation submitted. You can review your client after acceptance.",
                    resignationId = notice.ResignationId,
                    interviewId = notice.InterviewId,
                    noticeDays = ResignationWorkflowService.NoticeDays,
                    noticeEndsAtUtc = notice.NoticeEndsAtUtc,
                    lastWorkingDate = notice.LastWorkingDate,
                    status = "ResignationPending"
                });
            }
            catch (ResignationWorkflowException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex) { return WorkflowFailure(ex); }
        }

        [HttpGet("GetClientResignations")]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> GetClientResignations([FromServices] ResignationWorkflowService workflow, CancellationToken ct = default)
        {
            try
            {
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

                if (string.IsNullOrEmpty(userIdStr))
                    return Unauthorized(new { message = "Invalid user session." });

                int clientId = int.Parse(userIdStr);
                await workflow.ProcessDueAsync(clientId: clientId, ct: ct);

                var data = await _context.Resignations
                    .Include(r => r.Interview)
                        .ThenInclude(i => i.Worker) // Removed .ThenInclude(w => w.Category)
                    .Where(r => r.Interview != null && r.Interview.ClientId == clientId)
                    // Resignations the client still has to act on come first, newest first.
                    .OrderBy(r => r.Interview!.Status == "Resigned" ? 1 : 0)
                    .ThenByDescending(r => r.SubmittedDate)
                    .ToListAsync();

                var results = new List<object>();
                foreach (var r in data)
                {
                    var workerId = r.Interview?.WorkerId;
                    // Fetch the category name via the junction table safely
                    var workerRole = await _context.WorkerCategories
                        .Where(wc => wc.WorkerId == workerId)
                        .Join(_context.Categories, wc => wc.CategoryId, c => c.CategoryId, (wc, c) => c.CategoryName)
                        .FirstOrDefaultAsync() ?? "Worker";

                    int interviewId = r.InterviewId ?? 0;
                    var hasClientReview = await _context.Reviews.AnyAsync(rev => rev.InterviewId == interviewId && rev.ReviewerRole == "Client", ct);
                    var clientConfirmed = r.Interview?.Status == "Resigned" || hasClientReview;
                    var hasWorkerReview = await _context.Reviews.AnyAsync(rev => rev.InterviewId == interviewId && rev.ReviewerRole == "Worker");

                    results.Add(new
                    {
                        resignationId = r.ResignationId,
                        interviewId = interviewId,
                        status = r.Interview?.Status ?? "Pending",
                        isConfirmed = clientConfirmed,
                        isAccepted = ResignationWorkflowService.IsAccepted(r),
                        reviewSubmitted = hasClientReview,
                        canAccept = !ResignationWorkflowService.IsEnded(r.Interview?.Status),
                        acceptanceMode = r.AcceptanceMode,
                        acceptedAtUtc = r.AcceptedAtUtc,
                        noticeEndsAtUtc = r.NoticeEndsAtUtc,
                        hasWorkerReview = hasWorkerReview,
                        workerName = r.Interview?.Worker?.Name ?? "Unknown Worker",
                        workerRole = workerRole,
                        reason = r.ResignationReason,
                        lastWorkingDate = r.LastWorkingDate.ToString("MMM dd, yyyy"),
                        submittedDate = r.SubmittedDate != null ? r.SubmittedDate.Value.ToString("MMM dd, yyyy") : "N/A"
                    });
                }

                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching resignations: " + ex.Message });
            }
        }

        [HttpGet("GetResignationDetail/{id}")]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> GetResignationDetail(int id, [FromServices] ResignationWorkflowService workflow, CancellationToken ct = default)
        {
            if (ActorId is not int clientId) return Unauthorized();
            try
            {
                await workflow.ProcessDueAsync(clientId: clientId, ct: ct);
                var resignation = await _context.Resignations
                    .Include(r => r.Interview)
                        .ThenInclude(i => i.Worker) // Removed .ThenInclude(w => w.Category)
                    .FirstOrDefaultAsync(r => r.ResignationId == id && r.Interview != null && r.Interview.ClientId == clientId, ct);

                if (resignation == null)
                    return NotFound(new { message = "Resignation not found." });

                var worker = resignation.Interview?.Worker;

                // Fetch the category name via the junction table safely
                var workerRole = await _context.WorkerCategories
                    .Where(wc => wc.WorkerId == (worker != null ? worker.WorkerId : 0))
                    .Join(_context.Categories, wc => wc.CategoryId, c => c.CategoryId, (wc, c) => c.CategoryName)
                    .FirstOrDefaultAsync() ?? "Worker";

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
                var isAccepted = ResignationWorkflowService.IsAccepted(resignation);
                if (resignation.NoticeEndsAtUtc.HasValue)
                {
                    totalNoticeDays = ResignationWorkflowService.NoticeDays;
                    var secondsLeft = (resignation.NoticeEndsAtUtc.Value - workflow.UtcNow).TotalSeconds;
                    remainingDays = isAccepted ? 0 : Math.Max(0, (int)Math.Ceiling(secondsLeft / 86400d));
                    progress = isAccepted ? 1 : Math.Clamp(1 - secondsLeft / (7d * 86400d), 0, 1);
                }
                else if (isAccepted) { remainingDays = 0; progress = 1; }

                // Only the CLIENT's own closing review counts as "the client confirmed this".
                // The old check had no ReviewerRole filter, so the review the WORKER wrote while
                // submitting the resignation satisfied it — that is why the client's screen said
                // "This resignation has already been confirmed and is now readonly" and the
                // Confirm button was dead before the client had done anything.
                bool hasClientReview = await _context.Reviews.AnyAsync(r =>
                    r.InterviewId == resignation.InterviewId && r.ReviewerRole == "Client");

                bool isConfirmed = resignation.Interview?.Status == "Resigned" || hasClientReview;

                // Both sides of this contract's story. Legacy reviews submitted
                // before acceptance remain visible; new Worker reviews come later.
                int contractId = resignation.InterviewId ?? 0;

                var contractReviews = await _context.Reviews
                    .Where(r => r.InterviewId == resignation.InterviewId)
                    .Select(r => new
                    {
                        r.ReviewId,
                        r.ReviewerRole,
                        r.Rating,
                        r.Comment,
                        r.ReviewDate,
                        criteria = r.ReviewCriteriaRatings
                            .OrderBy(x => x.Criteria.SortOrder)
                            .ThenBy(x => x.CriteriaId)
                            .Select(x => new { name = x.Criteria.CriteriaName, score = x.Score })
                            .ToList()
                    })
                    .OrderByDescending(r => r.ReviewDate)
                    .ThenByDescending(r => r.ReviewId)
                    .ToListAsync();

                var clientReviewRow = contractReviews.FirstOrDefault(r => r.ReviewerRole == "Client");
                var workerReviewRow = contractReviews.FirstOrDefault(r => r.ReviewerRole == "Worker");

                // Worked period of this contract: Hiring_Date -> Last Working Day.
                var contractWindows = await LoadContractWindowsAsync(
                    new List<int> { contractId },
                    new Dictionary<int, DateTime?> { { contractId, clientReviewRow?.ReviewDate ?? workerReviewRow?.ReviewDate } });
                contractWindows.TryGetValue(contractId, out var contractWindow);
                var workedPeriod = FormatWorkedPeriod(contractWindow.From, contractWindow.To);

                object? workerReview = null;
                if (workerReviewRow != null)
                {
                    workerReview = new
                    {
                        endType = resignation.Interview?.Status,
                        endReasonType = contractWindow.EndReasonType,
                        endReason = contractWindow.EndReason,
                        rating = workerReviewRow.Rating ?? 0,
                        comment = workerReviewRow.Comment ?? "",
                        date = workerReviewRow.ReviewDate?.ToString("MMM dd, yyyy") ?? "N/A",
                        workedPeriod = workedPeriod,
                        criteria = workerReviewRow.criteria
                    };
                }

                object? clientReview = null;
                if (clientReviewRow != null)
                {
                    clientReview = new
                    {
                        endType = resignation.Interview?.Status,
                        endReasonType = contractWindow.EndReasonType,
                        endReason = contractWindow.EndReason,
                        rating = clientReviewRow.Rating ?? 0,
                        comment = clientReviewRow.Comment ?? "",
                        date = clientReviewRow.ReviewDate?.ToString("MMM dd, yyyy") ?? "N/A",
                        criteria = clientReviewRow.criteria
                    };
                }

                return Ok(new
                {
                    resignationId = resignation.ResignationId,
                    interviewId = resignation.InterviewId,
                    workerName = worker?.Name ?? "Unknown",
                    workerRole = workerRole,
                    workerAvatar = worker?.Picture,
                    reason = resignation.ResignationReason,
                    lastWorkingDate = lastDayRaw.ToString("MMM dd, yyyy"),
                    totalNoticeDays = totalNoticeDays,
                    remainingDays = remainingDays,
                    progress = Math.Round(progress, 2),
                    status = resignation.Interview?.Status ?? "Pending",
                    isConfirmed = isConfirmed,
                    isAccepted,
                    canAccept = !ResignationWorkflowService.IsEnded(resignation.Interview?.Status),
                    acceptanceMode = resignation.AcceptanceMode,
                    acceptedAtUtc = resignation.AcceptedAtUtc,
                    noticeEndsAtUtc = resignation.NoticeEndsAtUtc,
                    reviewSubmitted = hasClientReview,
                    workerReview = workerReview,
                    clientReview = clientReview,
                    workedPeriod = workedPeriod
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error: " + ex.Message });
            }
        }

        [HttpPost("ConfirmResignation")]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> ConfirmResignation([FromBody] ClosingReviewRequest model,
            [FromServices] ResignationWorkflowService workflow, CancellationToken ct = default)
        {
            if (ActorId is not int clientId) return Unauthorized();
            try
            {
                await workflow.ProcessDueAsync(clientId: clientId, ct: ct);
                var notice = await workflow.ConfirmWithClientReviewAsync(clientId, model.InterviewId,
                    model.ResignationId, async () =>
                    {
                        if (string.IsNullOrWhiteSpace(model.Comment))
                            throw new ResignationWorkflowException(400, "Please enter review remarks before confirming.");
                        var criteria = await BuildCriteriaRatingsAsync(model.InterviewId, model.CriteriaScores ?? new());
                        if (criteria.Error != null) throw new ResignationWorkflowException(400, criteria.Error);
                        return new Review { Rating = criteria.Average, Comment = model.Comment.Trim(), ReviewCriteriaRatings = criteria.Rows };
                    }, ct);
                return Ok(new
                {
                    message = notice.AcceptanceMode == "Automatic"
                        ? "The resignation was automatically accepted. Your review is now saved."
                        : "Resignation confirmed. Your review of the worker is saved.",
                    interviewId = notice.InterviewId,
                    resignationId = notice.ResignationId,
                    status = "Resigned",
                    notice.AcceptedAtUtc,
                    notice.AcceptanceMode
                });
            }
            catch (ResignationWorkflowException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex) { return WorkflowFailure(ex); }
        }

        [HttpPost("TerminateContract")]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> TerminateContract([FromBody] TerminationRequest request, [FromServices] ResignationWorkflowService workflow, CancellationToken ct = default)
        {
            if (ActorId is not int clientId) return Unauthorized();
            await using var transaction = await workflow.BeginUnitAsync(ct);
            try
            {
                // 1. Locate the Interview via the associated Hiring ID trace
                var interview = await workflow.LockInterviewAsync(request.InterviewId, ct);
                if (interview == null || interview.ClientId != clientId) return NotFound(new { message = "Job record not found." });

                // Contract can only be closed once, and only with one closing review —
                // otherwise a double tap produced two Termination rows plus two ratings.
                if (ResignationWorkflowService.IsEnded(interview.Status))
                    return BadRequest(new { message = "This contract has already been terminated." });

                var alreadyReviewed = await _context.Reviews.AnyAsync(r =>
                    r.InterviewId == request.InterviewId && r.ReviewerRole == "Client");
                if (alreadyReviewed)
                    return BadRequest(new { message = "A closing review already exists for this contract." });

                // Add the client's criteria-scored closing review to the review log.
                // The overall Rating is derived from the criteria, not sent by the client.
                var criteria = await BuildCriteriaRatingsAsync(request.InterviewId, request.CriteriaScores);
                if (criteria.Error != null)
                    return BadRequest(new { message = criteria.Error });

                var review = new Review
                {
                    InterviewId = request.InterviewId,
                    Rating = criteria.Average,
                    Comment = request.Remarks,
                    ReviewerRole = "Client",
                    ReviewDate = DateTime.Now,
                    ReviewCriteriaRatings = criteria.Rows
                };
                _context.Reviews.Add(review);

                // Keep the historic interview flag state as Terminated
                interview.Status = "Terminated";
                if (interview.WorkerId.HasValue)
                {
                    var worker = await _context.Workers.FindAsync(interview.WorkerId.Value);
                    if (worker != null)
                        worker.AvailableStatus = !await HasActiveExclusiveFullTimeJobAsync(worker.WorkerId, interview.InterviewId);
                }

                // 2. Point Termination toward Hiring instead of Interview mapping 
                // Note: verify if your entity model property name is precisely 'HiringId'
                var termination = new Termination
                {
                    InterviewId = request.InterviewId, // If DB schema isn't altered yet, keep this; otherwise mutate to t.HiringId = hiringId
                    TerminatedDate = DateOnly.FromDateTime(DateTime.Now),
                    TerminatedReason = request.Reason
                };
                _context.Terminations.Add(termination);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync(ct);

                return Ok(new { message = "Contract terminated successfully." });
            }
            catch (Exception ex)
            {
                // Disposing the workflow unit rolls back any uncommitted SQL writes.
                return StatusCode(500, new { message = "Error: " + ex.Message });
            }
        }

        [HttpGet("GetActiveRequests/{clientId}")]
        public async Task<IActionResult> GetActiveRequests(int clientId)
        {
            try
            {
                // Explicitly exclude "Terminated" status records alongside Hired, JobRejected, and Finalized states
                var requests = await _context.Interviews
                    .Include(i => i.Worker)
                    .Where(i => i.ClientId == clientId
                             && i.Status != "Hired"
                             && i.Status != "JobRejected"
                             && i.Status != "Finalized"
                             && i.Status != "Terminated") // <-- Added this vital filter inclusion entry
                    .Select(i => new
                    {
                        interviewId = i.InterviewId,
                        workerDecision = i.WorkerDecision ?? "Pending",
                        hiringStatus = _context.Hiring.Where(h => h.InterviewId == i.InterviewId)
                                                      .Select(h => h.HiringDecision)
                                                      .FirstOrDefault() ?? "Pending",
                        workerName = i.Worker != null ? i.Worker.Name : "Unknown",
                        workerImage = i.Worker != null ? i.Worker.Picture : null,
                        workerSkill = _context.WorkerCategories.Where(wc => wc.WorkerId == i.WorkerId)
                                                              .Join(_context.Categories, wc => wc.CategoryId, c => c.CategoryId, (wc, c) => c.CategoryName)
                                                              .FirstOrDefault() ?? "Worker",
                        status = i.Status,
                        jobType = i.JobType ?? "Full-Time",
                        slotId = i.SlotId,
                        slotStartTime = _context.WorkerTimeSlots.Where(s => s.Id == i.SlotId).Select(s => (TimeSpan?)s.StartTime).FirstOrDefault(),
                        slotEndTime = _context.WorkerTimeSlots.Where(s => s.Id == i.SlotId).Select(s => (TimeSpan?)s.EndTime).FirstOrDefault(),
                    })
                    .ToListAsync();

                return Ok(requests);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching requests: " + ex.Message });
            }
        }

        [HttpGet("GetLatestTermination/{workerId}")]
        [Authorize(Roles = "Worker")]
        public async Task<IActionResult> GetLatestTermination(int workerId, [FromQuery] int? terminationId = null)
        {
            try
            {
                var rawId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
                if (!int.TryParse(rawId, out int authenticatedId) || authenticatedId <= 0) return Unauthorized();
                if (authenticatedId != workerId) return Forbid();
                if (terminationId.HasValue && terminationId.Value <= 0) return BadRequest();
                var termination = await _context.Terminations
                    .Include(t => t.Interview)
                        .ThenInclude(i => i.Client)
                    .Where(t => t.Interview != null && t.Interview.WorkerId == workerId &&
                        (!terminationId.HasValue || t.TerminationId == terminationId.Value))
                    .OrderByDescending(t => t.TerminatedDate)
                    .ThenByDescending(t => t.TerminationId)
                    .Select(t => new
                    {
                        t.TerminationId,
                        t.InterviewId,
                        t.TerminatedDate,
                        t.TerminatedReason,
                        ClientName = t.Interview.Client.Name,
                        ClientPicture = t.Interview.Client.Picture,
                        Status = t.Interview.Status,
                        // Lets WorkerTerminationScreen hide the star form when the worker
                        // already rated this client (one review per side per contract).
                        workerReviewSubmitted = _context.Reviews.Any(r =>
                            r.InterviewId == t.InterviewId && r.ReviewerRole == "Worker")
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


        [HttpPut("UpdateDutyStatus/{workerId}")]
        [Authorize(Roles = "Worker")]
        public async Task<IActionResult> UpdateDutyStatus(int workerId, [FromBody] bool isAvailable)
        {
            try
            {
                if (ActorId != workerId) return Forbid();
                var worker = await _context.Workers.FindAsync(workerId);
                if (worker == null) return NotFound(new { message = "Worker not found." });
                if (isAvailable && await HasActiveExclusiveFullTimeJobAsync(workerId))
                    return Conflict(new { code = "EXCLUSIVE_JOB_LOCK", message = "Availability is locked while your exclusive full-time job is active. Submit a resignation or wait for termination to unlock it." });

                worker.AvailableStatus = isAvailable;
                await _context.SaveChangesAsync();

                return Ok(new { status = "Success", message = "Duty status updated!", isAvailable = worker.AvailableStatus });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating duty status: " + ex.Message });
            }
        }
        [HttpPost("FinalizeHiringDecision")]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> FinalizeHiringDecision([FromBody] HiringUpdateDto model)
        {
            try
            {
                var clientId = ActorId;
                if (clientId == null) return Unauthorized();
                var hiring = await _context.Hiring.Include(h => h.Interview)
                    .FirstOrDefaultAsync(h => h.HiringId == model.HiringId && h.Interview != null && h.Interview.ClientId == clientId);
                if (hiring == null) return NotFound(new { message = "Hiring context record not found." });
                if (hiring.WorkerDecision != "Accepted") return Conflict(new { message = "Worker has not accepted this offer." });
                var interview = hiring.Interview!;
                await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
                if (interview.JobType == "Full-Time" && interview.WorkerId.HasValue)
                {
                    await _context.Workers.FromSqlInterpolated($"SELECT * FROM [dbo].[Worker] WITH (UPDLOCK, ROWLOCK) WHERE [Worker_ID] = {interview.WorkerId.Value}").FirstOrDefaultAsync();
                    if (await HasActiveFullTimeJobAsync(interview.WorkerId.Value, interview.InterviewId))
                        return Conflict(new { code = "ACTIVE_FULL_TIME_JOB", message = "Worker already has an active full-time job. They must leave it before this full-time offer can be confirmed." });
                }

                // Client finalization is the point at which exclusive status locks availability.
                hiring.HiringDecision = "Accepted";
                hiring.HiringDate = DateTime.Now;

                interview.Status = "Finalized";
                if (interview.JobType == "Full-Time" && interview.IsExclusive && interview.WorkerId.HasValue)
                {
                    var worker = await _context.Workers.FindAsync(interview.WorkerId.Value);
                    if (worker != null) worker.AvailableStatus = false;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { status = "Success", message = "Hiring handshake step state successfully mutated." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal transaction failure: " + ex.Message });
            }
        }
        [HttpPost("update-location")]
        public async Task<IActionResult> UpdateClientLocation([FromBody] ClientLocationDTO dto)
        {
            try
            {
                if (dto == null || dto.Latitude == 0 || dto.Longitude == 0)
                    return BadRequest(new { message = "Invalid latitude or longitude." });

                var client = await _context.Clients.FirstOrDefaultAsync(c => c.ClientId == dto.ClientId);
                if (client == null)
                    return NotFound(new { message = "Client not found." });

                // Save coordinates directly
                client.Latitude = dto.Latitude;
                client.Longitude = dto.Longitude;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Location updated successfully",
                    latitude = client.Latitude,
                    longitude = client.Longitude
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        // DTO Class
        public class ClientLocationDTO
        {
            public int ClientId { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
        }

        public class HiringUpdateDto
        {
            public int HiringId { get; set; }
            public string HiringDecision { get; set; } = null!;
        }

        public class TerminationRequest
        {
            public int InterviewId { get; set; }
            public string Reason { get; set; } = null!;
            public string? Remarks { get; set; }

            /// <summary>
            /// Legacy field. The overall is now derived from CriteriaScores, so this
            /// is ignored on the client -> worker closing review. Kept so older
            /// clients that still post it do not fail model binding.
            /// </summary>
            public int Rating { get; set; }

            public List<CriteriaScoreDto> CriteriaScores { get; set; } = new();
        }

        [HttpGet("GetWorkerResignationReview/{id}")]
        [Authorize(Roles = "Worker")]
        public async Task<IActionResult> GetWorkerResignationReview(int id,
            [FromServices] ResignationWorkflowService workflow, CancellationToken ct = default)
        {
            if (ActorId is not int workerId) return Unauthorized();
            try
            {
                await workflow.ProcessDueAsync(workerId: workerId, ct: ct);
                var notice = await _context.Resignations.AsNoTracking().Include(r => r.Interview).ThenInclude(i => i!.Client)
                    .FirstOrDefaultAsync(r => r.ResignationId == id && r.Interview != null && r.Interview.WorkerId == workerId, ct);
                if (notice?.InterviewId is not int interviewId) return NotFound(new { message = "Resignation not found." });
                var review = await _context.Reviews.AsNoTracking().Where(r => r.InterviewId == interviewId && r.ReviewerRole == "Worker")
                    .OrderByDescending(r => r.ReviewDate).ThenByDescending(r => r.ReviewId).FirstOrDefaultAsync(ct);
                var accepted = ResignationWorkflowService.IsAccepted(notice);
                var windows = await LoadContractWindowsAsync(new List<int> { interviewId },
                    new Dictionary<int, DateTime?> { { interviewId, review?.ReviewDate } });
                windows.TryGetValue(interviewId, out var window);
                return Ok(new
                {
                    resignationId = id,
                    interviewId,
                    clientName = notice.Interview!.Client?.Name ?? "Client",
                    clientPicture = notice.Interview.Client?.Picture,
                    reason = notice.ResignationReason,
                    status = notice.Interview.Status,
                    isAccepted = accepted,
                    canReview = accepted && review == null,
                    notice.AcceptedAtUtc,
                    notice.AcceptanceMode,
                    notice.NoticeEndsAtUtc,
                    workerReview = review == null ? null : new
                    {
                        rating = review.Rating,
                        comment = review.Comment ?? "",
                        date = review.ReviewDate?.ToString("MMM dd, yyyy"),
                        workedPeriod = FormatWorkedPeriod(window.From, window.To),
                        endReasonType = window.EndReasonType,
                        endReason = window.EndReason
                    }
                });
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex) { return WorkflowFailure(ex); }
        }

        [HttpPost("SubmitWorkerReviewToClient")]
        [Authorize(Roles = "Worker")]
        public async Task<IActionResult> SubmitWorkerReviewToClient([FromBody] Review model,
            [FromServices] ResignationWorkflowService workflow, CancellationToken ct = default)
        {
            if (ActorId is not int workerId) return Unauthorized();
            try
            {
                await workflow.ProcessDueAsync(workerId: workerId, ct: ct);
                var review = await workflow.SubmitWorkerReviewAsync(workerId, model.InterviewId ?? 0, model.Rating, model.Comment, ct);
                return Ok(new { message = "Review submitted successfully.", reviewId = review.ReviewId });
            }
            catch (ResignationWorkflowException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex) { return WorkflowFailure(ex); }
        }

        [HttpGet("GetClientProfile/{clientId}")]
        public async Task<IActionResult> GetClientProfile(int clientId)
        {
            try
            {
                var client = await _context.Clients.FindAsync(clientId);
                if (client == null) return NotFound(new { message = "Client not found." });

                var reviews = await _context.Reviews
                    .Include(r => r.Interview)
                        .ThenInclude(i => i.Worker)
                    .Where(r => r.Interview != null && r.Interview.ClientId == clientId && r.ReviewerRole == "Worker")
                    .Select(r => new {
                        r.ReviewId,
                        interviewId = r.InterviewId ?? 0,
                        reviewDateRaw = r.ReviewDate,
                        reviewerName = r.Interview!.Worker != null ? r.Interview.Worker.Name : "Anonymous Worker",
                        endType = r.Interview.Status,
                        rating = r.Rating,
                        comment = r.Comment,
                        date = r.ReviewDate != null ? r.ReviewDate.Value.ToString("MMM dd, yyyy") : "N/A",
                        criteria = r.ReviewCriteriaRatings
                            .OrderBy(x => x.Criteria.SortOrder)
                            .ThenBy(x => x.CriteriaId)
                            .Select(x => new { name = x.Criteria.CriteriaName, score = x.Score })
                            .ToList()
                    })
                    // Order on the real columns — the old string sort on the formatted date put
                    // "Jan 05, 2026" before "Dec 30, 2025".
                    .OrderByDescending(r => r.reviewDateRaw)
                    .ThenByDescending(r => r.ReviewId)
                    .ToListAsync();

                // Worked period on every review: Hiring_Date -> Termination / Resignation date.
                var profileReviewWindows = await LoadContractWindowsAsync(
                    reviews.Select(r => r.interviewId).Distinct().ToList(),
                    reviews.GroupBy(r => r.interviewId).ToDictionary(g => g.Key, g => g.Max(r => r.reviewDateRaw)));

                var reviewPayload = reviews.Select(r =>
                {
                    profileReviewWindows.TryGetValue(r.interviewId, out var window);
                    return new
                    {
                        id = r.ReviewId.ToString(),
                        reviewerName = r.reviewerName,
                        endType = r.endType,
                        endReasonType = window.EndReasonType,
                        endReason = window.EndReason,
                        rating = r.rating,
                        comment = r.comment,
                        criteria = r.criteria,
                        date = r.date,
                        workedFrom = window.From?.ToString("yyyy-MM-dd"),
                        workedTo = window.To?.ToString("yyyy-MM-dd"),
                        workedPeriod = FormatWorkedPeriod(window.From, window.To)
                    };
                }).ToList();

                double avgRating = reviewPayload.Any() ? Math.Round(reviewPayload.Average(r => (double)(r.rating ?? 0)), 1) : 0.0;

                return Ok(new
                {
                    clientId = client.ClientId,
                    name = client.Name,
                    phone = client.Phone,
                    address = client.Address,
                    picture = client.Picture,
                    rating = avgRating,
                    reviewCount = reviewPayload.Count,
                    reviews = reviewPayload
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching client profile: " + ex.Message });
            }
        }
        [HttpGet("GetClientDetail/{clientId}")]
        public async Task<IActionResult> GetClientDetail(int clientId)
        {
            try
            {
                var client = await _context.Clients
                    .FirstOrDefaultAsync(c => c.ClientId == clientId);

                if (client == null)
                    return NotFound(new { message = "Client profile not found." });

                // Fetch reviews left by Workers for this Client
                var workerReviewsOfClient = await _context.Reviews
                    .Include(r => r.Interview)
                        .ThenInclude(i => i.Worker)
                    .Where(r => r.Interview.ClientId == clientId && r.ReviewerRole == "Worker")
                    // Order on the real columns (the old `.OrderByDescending(r => r.id)` was a
                    // STRING sort on the projected ReviewId, so "9" beat "10")
                    .OrderByDescending(r => r.ReviewDate)
                    .ThenByDescending(r => r.ReviewId)
                    .Select(r => new
                    {
                        r.ReviewId,
                        interviewId = r.InterviewId ?? 0,
                        reviewDateRaw = r.ReviewDate,
                        reviewerName = r.Interview.Worker != null ? r.Interview.Worker.Name : "Anonymous Worker",
                        reviewerImage = r.Interview.Worker != null ? r.Interview.Worker.Picture : "",
                        endType = r.Interview.Status,
                        rating = r.Rating ?? 0,
                        comment = r.Comment ?? "",
                        date = r.ReviewDate.HasValue ? r.ReviewDate.Value.ToString("MMM dd, yyyy") : "N/A",
                        criteria = r.ReviewCriteriaRatings
                            .OrderBy(x => x.Criteria.SortOrder)
                            .ThenBy(x => x.CriteriaId)
                            .Select(x => new { name = x.Criteria.CriteriaName, score = x.Score })
                            .ToList()
                    })
                    .ToListAsync();

                // Worked period on every review: Hiring_Date -> Termination / Resignation date.
                var clientReviewWindows = await LoadContractWindowsAsync(
                    workerReviewsOfClient.Select(r => r.interviewId).Distinct().ToList(),
                    workerReviewsOfClient.GroupBy(r => r.interviewId).ToDictionary(g => g.Key, g => g.Max(r => r.reviewDateRaw)));

                var reviewPayload = workerReviewsOfClient.Select(r =>
                {
                    clientReviewWindows.TryGetValue(r.interviewId, out var window);
                    return new
                    {
                        id = r.ReviewId.ToString(),
                        reviewerName = r.reviewerName,
                        reviewerImage = r.reviewerImage,
                        endType = r.endType,
                        endReasonType = window.EndReasonType,
                        endReason = window.EndReason,
                        rating = r.rating,
                        comment = r.comment,
                        criteria = r.criteria,
                        date = r.date,
                        workedFrom = window.From?.ToString("yyyy-MM-dd"),
                        workedTo = window.To?.ToString("yyyy-MM-dd"),
                        workedPeriod = FormatWorkedPeriod(window.From, window.To)
                    };
                }).ToList();

                double avgRating = reviewPayload.Any()
                    ? Math.Round(reviewPayload.Average(r => (double)r.rating), 1)
                    : 0.0;

                return Ok(new
                {
                    clientId = client.ClientId,
                    name = client.Name,
                    email = client.Email,
                    phone = client.Phone,
                    address = client.Address,
                    picture = client.Picture,
                    rating = avgRating,
                    reviewCount = reviewPayload.Count,
                    reviews = reviewPayload
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error loading client profile: " + ex.Message });
            }
        }
        [HttpPut("UpdateWorkerLocation")]
        public async Task<IActionResult> UpdateWorkerLocation([FromBody] UpdateWorkerLocationDto dto)
        {
            var worker = await _context.Workers.FindAsync(dto.WorkerId);
            if (worker == null) return NotFound("Worker not found.");

            worker.Latitude = dto.Latitude;
            worker.Longitude = dto.Longitude;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Worker location updated successfully." });
        }
        [HttpGet("GetWorkerTimeSlots/{workerId}")]
        public async Task<IActionResult> GetWorkerTimeSlots(int workerId)
        {
            try
            {
                var slots = await _context.WorkerTimeSlots
                    .Where(s => s.WorkerId == workerId)
                    .Select(s => new
                    {
                        s.Id,
                        s.WorkerId,
                        StartTime = DateTime.Today.Add(s.StartTime).ToString("hh:mm tt"),
                        EndTime = DateTime.Today.Add(s.EndTime).ToString("hh:mm tt"),
                        RawStartTime = s.StartTime.ToString(@"hh\:mm"),
                        RawEndTime = s.EndTime.ToString(@"hh\:mm")
                    })
                    .ToListAsync();

                return Ok(slots);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving time slots: " + ex.Message });
            }
        }

        // 2. POST: Add a new time slot for a worker
        [HttpPost("AddTimeSlot")]
        public async Task<IActionResult> AddTimeSlot([FromBody] WorkerTimeSlots slot)
        {
            try
            {
                if (slot == null || slot.WorkerId <= 0)
                {
                    return BadRequest(new { message = "Invalid time slot data." });
                }

                _context.WorkerTimeSlots.Add(slot);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Time slot added successfully!", slotId = slot.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error adding time slot: " + ex.Message });
            }
        }

        // 3. DELETE: Remove a time slot
        [HttpDelete("DeleteTimeSlot/{id}")]
        public async Task<IActionResult> DeleteTimeSlot(int id)
        {
            try
            {
                var slot = await _context.WorkerTimeSlots.FindAsync(id);
                if (slot == null)
                {
                    return NotFound(new { message = "Time slot not found." });
                }

                _context.WorkerTimeSlots.Remove(slot);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Time slot deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting time slot: " + ex.Message });
            }
        }

        [HttpPut("UpdateWorkerRadius/{workerId}")]
        public async Task<IActionResult> UpdateWorkerRadius(int workerId, [FromBody] int radius)
        {
            try
            {
                if (radius < 1 || radius > 70)
                    return BadRequest(new { message = "Invalid radius distance." });

                var worker = await _context.Workers.FindAsync(workerId);
                if (worker == null) return NotFound(new { message = "Worker not found." });

                worker.Radius = radius;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Work radius updated successfully", radius = worker.Radius });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating radius: " + ex.Message });
            }
        }

        // ─── CRITERIA-BASED CLOSING REVIEWS ─────────────────────────────────────
        // The profession a review is about is derived, never stored:
        //   Interview -> Worker -> Worker_Category -> Category -> ReviewCriteria
        // so no Category_ID column is needed on Interview or Reviews.

        /// <summary>
        /// Returns the criteria the client must score when closing the contract
        /// behind <paramref name="interviewId"/>: the common set plus every
        /// criterion belonging to a category that worker holds.
        /// </summary>
        [HttpGet("GetReviewCriteria")]
        public async Task<IActionResult> GetReviewCriteria([FromQuery] int interviewId)
        {
            try
            {
                var workerId = await _context.Interviews
                    .Where(i => i.InterviewId == interviewId)
                    .Select(i => i.WorkerId)
                    .FirstOrDefaultAsync();

                if (workerId == null)
                    return NotFound(new { message = "Interview not found." });

                var criteria = await LoadApplicableCriteriaAsync(workerId.Value);

                var common = criteria
                    .Where(c => c.CategoryId == null)
                    .Select(c => new { criteriaId = c.CriteriaId, name = c.CriteriaName })
                    .ToList();

                var professions = criteria
                    .Where(c => c.CategoryId != null)
                    .GroupBy(c => new { c.CategoryId, c.CategoryName })
                    .Select(g => new
                    {
                        categoryId = g.Key.CategoryId,
                        categoryName = g.Key.CategoryName,
                        criteria = g.Select(c => new { criteriaId = c.CriteriaId, name = c.CriteriaName }).ToList()
                    })
                    .ToList();

                return Ok(new
                {
                    interviewId,
                    workerId,
                    common,
                    professions,
                    totalCriteria = common.Count + professions.Sum(p => p.criteria.Count),
                    allMandatory = true
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error loading review criteria: " + ex.Message });
            }
        }

        /// <summary>
        /// Every active criterion that applies to this worker: the common set
        /// (CategoryId == null) plus each criterion scoped to a category the
        /// worker actually holds.
        /// </summary>
        private async Task<List<CriteriaProjection>> LoadApplicableCriteriaAsync(int workerId)
        {
            // Worker_Category is a (worker, category, skill) triple, so a worker with
            // three skills in one category still resolves to exactly one distinct
            // category here. Distinct() also covers the rare multi-category worker.
            var categoryIds = await _context.WorkerCategories
                .Where(wc => wc.WorkerId == workerId)
                .Select(wc => wc.CategoryId)
                .Distinct()
                .ToListAsync();

            return await _context.ReviewCriteria
                .Where(c => c.IsActive && (c.CategoryId == null || categoryIds.Contains(c.CategoryId!.Value)))
                .OrderBy(c => c.CategoryId)   // NULLs first in SQL Server => common criteria lead
                .ThenBy(c => c.SortOrder)
                .Select(c => new CriteriaProjection
                {
                    CriteriaId = c.CriteriaId,
                    CategoryId = c.CategoryId,
                    CategoryName = c.Category != null ? c.Category.CategoryName : null,
                    CriteriaName = c.CriteriaName,
                    SortOrder = c.SortOrder
                })
                .ToListAsync();
        }

        /// <summary>
        /// Validates the submitted scores against the worker's applicable criteria
        /// (all mandatory, nothing extra, each 1-5) and returns the rows to persist
        /// plus the overall average that goes into Reviews.Rating.
        /// </summary>
        private async Task<CriteriaBuildResult> BuildCriteriaRatingsAsync(
            int? interviewId, List<CriteriaScoreDto> submitted)
        {
            var result = new CriteriaBuildResult();

            var workerId = await _context.Interviews
                .Where(i => i.InterviewId == interviewId)
                .Select(i => i.WorkerId)
                .FirstOrDefaultAsync();

            if (workerId == null)
            {
                result.Error = "Interview or its worker could not be found.";
                return result;
            }

            var applicable = await LoadApplicableCriteriaAsync(workerId.Value);
            var requiredIds = applicable.Select(c => c.CriteriaId).ToHashSet();
            var submittedIds = submitted.Select(s => s.CriteriaId).ToHashSet();

            if (submitted.Count == 0)
            {
                result.Error = "No criteria scores were submitted.";
                return result;
            }

            var missing = requiredIds.Except(submittedIds).ToList();
            if (missing.Count > 0)
            {
                result.Error = "Please rate every criterion. Missing criteria: " + string.Join(", ", missing);
                return result;
            }

            var unexpected = submittedIds.Except(requiredIds).ToList();
            if (unexpected.Count > 0)
            {
                result.Error = "Criteria that do not apply to this worker were submitted: " + string.Join(", ", unexpected);
                return result;
            }

            if (submittedIds.Count != submitted.Count)
            {
                result.Error = "The same criterion was scored more than once.";
                return result;
            }

            if (submitted.Any(s => s.Score < 1 || s.Score > 5))
            {
                result.Error = "Each criterion must be scored between 1 and 5.";
                return result;
            }

            result.Rows = submitted
                .Select(s => new ReviewCriteriaRating { CriteriaId = s.CriteriaId, Score = s.Score })
                .ToList();

            // Overall = mean of the criteria scores, rounded to 2dp to fit decimal(3,2).
            result.Average = Math.Round((decimal)submitted.Average(s => s.Score), 2);
            return result;
        }

        public class CriteriaScoreDto
        {
            public int CriteriaId { get; set; }
            public int Score { get; set; }
        }

        /// <summary>Body for the two client-side closing reviews.</summary>
        public class ClosingReviewRequest
        {
            public int InterviewId { get; set; }
            public int? ResignationId { get; set; }
            public string? Comment { get; set; }
            public List<CriteriaScoreDto> CriteriaScores { get; set; } = new();
        }

        public class CriteriaBuildResult
        {
            public decimal Average { get; set; }
            public List<ReviewCriteriaRating> Rows { get; set; } = new();
            public string? Error { get; set; }
        }

        public class CriteriaProjection
        {
            public int CriteriaId { get; set; }
            public int? CategoryId { get; set; }
            public string? CategoryName { get; set; }
            public string CriteriaName { get; set; } = null!;
            public int SortOrder { get; set; }
        }

    }
    public class UpdateWorkerLocationDto
    {
        public int WorkerId { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
    }
    public class HiringDto
    {
        public int InterviewId { get; set; }
        public string? HiringDecision { get; set; }
        public string? Address { get; set; }
    }
}


