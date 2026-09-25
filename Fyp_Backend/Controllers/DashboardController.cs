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
    [FromQuery] List<string>? subSkills = null)
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
                    var workerCategoryNames = await _context.WorkerCategories
                        .Where(wc => wc.WorkerId == w.WorkerId)
                        .Join(_context.Categories,
                              wc => wc.CategoryId,
                              c => c.CategoryId,
                              (wc, c) => c.CategoryName)
                        .Where(name => !string.IsNullOrEmpty(name))
                        .Distinct()
                        .ToListAsync();

                    // Check Police Records Status
                    bool isFlagged = await _context.PoliceRecords
                        .AnyAsync(pr => pr.WorkerID == w.WorkerId && pr.IsFlagged == true);

                    bool isBlocked = await _context.PoliceRecords
                        .AnyAsync(pr => pr.WorkerID == w.WorkerId && pr.IsBlocked == true);

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
                        isFlagged = isFlagged,
                        isBlocked = isBlocked
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
                            clientId = i.ClientId,
                            reviewerName = i.Client?.Name ?? "Anonymous",
                            rating = r.Rating,
                            comment = r.Comment,
                            date = r.ReviewDate?.ToString("MMM dd, yyyy") ?? "N/A"
                        }))
                    .ToList();

                double avgRating = allReviews.Any() ? Math.Round(allReviews.Average(r => (double)(r.rating ?? 0)), 1) : 0.0;

                int pendingRequestCount = worker.Interviews.Count(i => i.WorkerDecision == null || i.WorkerDecision == "Pending");
                int jobNotificationCount = await _context.Hiring.CountAsync(h => h.Interview.WorkerId == worker.WorkerId && h.WorkerDecision == "Pending");
                int terminationCount = worker.Interviews.Count(i => i.Status == "Terminated");

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
                    pendingRequestCount = pendingRequestCount,
                    jobNotificationCount = jobNotificationCount,
                    terminationCount = terminationCount,
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
        // FIXED: was `.OrderByDescending(r => r.id)` on the string id, so
        // "9" sorted after "10". Newest review is now genuinely first.
        .OrderByDescending(r => r.ReviewDate)
        .ThenByDescending(r => r.ReviewId)
        .Select(r => new
        {
            id = r.ReviewId.ToString(),
            clientId = i.ClientId,
            name = i.Client != null ? i.Client.Name : "Client",
            rating = r.Rating ?? 0,
            comment = r.Comment ?? "",
            date = r.ReviewDate?.ToString("MMM dd, yyyy") ?? "N/A",
            duration = "Previous Client"
        }))
    .ToList();

                double avgRating = clientReviews.Any() ? Math.Round(clientReviews.Average(r => (double)r.rating), 1) : 0.0;

                return Ok(new
                {
                    averageRating = avgRating,
                    reviewCount = clientReviews.Count,
                    reviews = clientReviews
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

        [HttpPost("BookInterview")]
        public async Task<IActionResult> BookInterview([FromBody] Interview model)
        {
            try
            {
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
                // ────────────────────────────────────────────────────────────────────

                // ─── 6. Address fallback so the worker's card never shows "N/A" ─────
                if (string.IsNullOrWhiteSpace(model.Address))
                    model.Address = bookingClient.Address;

                _context.Interviews.Add(model);
                await _context.SaveChangesAsync();

                // Echo the created id + the frozen type back to the app
                return Ok(new
                {
                    message = $"{model.JobType} interview request booked successfully!",
                    interviewId = model.InterviewId,
                    jobType = model.JobType,
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
        public async Task<IActionResult> GetClientDashboard()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int clientId))
                {
                    return BadRequest(new { message = "Invalid user session token context." });
                }

                // 1. Calculate active pending initial interview inquiries
                int pendingCount = await _context.Interviews
                    .CountAsync(i => i.ClientId == clientId && i.Status == "Pending");

                // 2. Count only workers who are not fully terminated
                int workersCount = await _context.Hiring
                    .CountAsync(h => h.Interview!.ClientId == clientId &&
                                     h.WorkerDecision == "Accepted" &&
                                     h.HiringDecision == "Accepted" &&
                                     h.Interview.Status != "Terminated");

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
                        date = h.HiringDate != null ? h.HiringDate.Value.ToString("yyyy-MM-dd") : "",
                        status = h.Interview!.Status == "ResignationPending" ? "Pending Resignation"
                                : h.Interview!.Status == "Resigned" ? "Resigned"
                                : h.Interview!.Status == "Terminated" ? "Terminated"
                                : "On Work",
                        type = h.Interview!.Status == "ResignationPending" ? "alert"
                             : h.Interview!.Status == "Resigned" ? "resigned"
                             : h.Interview!.Status == "Terminated" ? "terminated"
                             : "active"
                    })
                    .ToListAsync();

                return Ok(new
                {
                    hiredCount = workersCount,
                    pendingInterviewsCount = pendingCount,
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
                        clientId=i.Client.ClientId,
                        location = i.Address ?? "N/A",
                        timeRaw = i.InterviewDate,
                        time = i.InterviewDate != null ? i.InterviewDate.Value.ToString("MMM dd, hh:mm tt") : "Not Set",
                        service = "Interview Request",
                        jobType = i.JobType ?? "Full-Time",
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
        public async Task<IActionResult> GetWorkerJobConfirmations()
        {
            try
            {
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

                if (string.IsNullOrEmpty(userIdStr))
                    return Unauthorized(new { message = "Invalid user session." });

                int workerId = int.Parse(userIdStr);

                var jobs = await _context.Hiring
                    .Include(h => h.Interview)
                        .ThenInclude(i => i.Client)
                    .Include(h => h.Interview)
                        .ThenInclude(i => i.Worker)
                    .Where(h => h.Interview.WorkerId == workerId)
                    .Select(h => new
                    {
                        id = h.InterviewId.ToString(),
                        clientName = h.Interview.Client != null ? h.Interview.Client.Name : "Client",
                        clientId = h.Interview.Client != null ? h.Interview.Client.ClientId : (int?)null,
                        status = h.Interview.Status,
                        date = h.HiringDate != null ? h.HiringDate.Value.ToString("dd-MM-yyyy") : "Pending",
                        role = _context.WorkerCategories.Where(wc => wc.WorkerId == h.Interview.WorkerId).Join(_context.Categories, wc => wc.CategoryId, c => c.CategoryId, (wc, c) => c.CategoryName).FirstOrDefault() ?? "Worker",
                        address = h.Address ?? "Pending",
                        hiringDecision = h.HiringDecision ?? "Pending",
                        workerDecision = h.WorkerDecision ?? "Pending",
                        jobType = h.Interview.JobType ?? "Full-Time",
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
                    if (item.workerDecision == "Rejected")
                    {
                        type = "rejected";
                        msg = "Thank you for your time. Job offer declined.";
                        displayStatus = "Rejected";
                    }
                    else if (item.status == "Terminated")
                    {
                        type = "terminated";
                        msg = "Your contract has been terminated by the client.";
                        displayStatus = "Terminated";
                    }
                    else if (item.hiringDecision == "Accepted")
                    {
                        type = "final";
                        msg = "Congratulations! You are officially hired. Welcome aboard!";
                        displayStatus = "Hired";
                    }
                    else if (item.workerDecision == "Accepted")
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
                        clientId = item.clientId, // <-- ADD THIS LINE
                        clientName = item.clientName,
                        clientRating = item.clientRating,
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
                var hiring = await _context.Hiring.Include(h => h.Interview).FirstOrDefaultAsync(h => h.InterviewId == id);
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
        public async Task<IActionResult> WorkerRejectJobOffer(int id)
        {
            try
            {
                var hiring = await _context.Hiring.Include(h => h.Interview).FirstOrDefaultAsync(h => h.InterviewId == id);
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
        public async Task<IActionResult> ClientConfirmWorkerAcceptance(int id)
        {
            try
            {
                var interview = await _context.Interviews.FindAsync(id);
                if (interview == null) return NotFound(new { message = "Record not found." });

                var hiring = await _context.Hiring.FirstOrDefaultAsync(h => h.InterviewId == id);
                if (hiring == null) return NotFound(new { message = "Hiring deployment card context absent." });

                // Rule 2d: if user also accepted, fill all fields
                interview.Status = "Finalized";
                hiring.HiringDecision = "Accepted";
                hiring.HiringDate = DateTime.Now;
                hiring.Address = interview.Address ?? "Confirmed Fleet Address Location";

                await _context.SaveChangesAsync();
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
        public async Task<IActionResult> GetActiveJob(int workerId)
        {
            try
            {
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
                        hireDate = x.Interview.InterviewDate
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
        public async Task<IActionResult> SubmitResignation([FromBody] Resignation model)
        {
            try
            {
                if (model == null || string.IsNullOrEmpty(model.ResignationReason))
                    return BadRequest(new { message = "Resignation reason is required." });

                // Load interview AND its worker so we can update both
                var interview = await _context.Interviews
                    .Include(i => i.Worker)
                    .FirstOrDefaultAsync(i => i.InterviewId == model.InterviewId);

                if (interview == null)
                    return NotFound(new { message = "Job record not found." });

                var alreadyResigned = await _context.Resignations
                    .AnyAsync(r => r.InterviewId == model.InterviewId);
                if (alreadyResigned)
                    return BadRequest(new { message = "You have already submitted a resignation for this job." });

                // 1. Insert the resignation notice record
                var resignation = new Resignation
                {
                    InterviewId = model.InterviewId,
                    ResignationReason = model.ResignationReason,
                    LastWorkingDate = model.LastWorkingDate,
                    SubmittedDate = DateTime.Now
                };
                _context.Resignations.Add(resignation);

                // 2. Keep the resignation in a pending state until the client confirms it
                interview.Status = "ResignationPending";

                // 3. Free up the worker so they appear in search results again
                if (interview.Worker != null)
                {
                    interview.Worker.AvailableStatus = true;
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "Resignation submitted successfully." });
            }
            catch (Exception ex)
            {
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
                        .ThenInclude(i => i.Worker) // Removed .ThenInclude(w => w.Category)
                    .Where(r => r.Interview != null && r.Interview.ClientId == clientId)
                    .OrderByDescending(r => r.SubmittedDate)
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

                    results.Add(new
                    {
                        resignationId = r.ResignationId,
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
        public async Task<IActionResult> GetResignationDetail(int id)
        {
            try
            {
                var resignation = await _context.Resignations
                    .Include(r => r.Interview)
                        .ThenInclude(i => i.Worker) // Removed .ThenInclude(w => w.Category)
                    .FirstOrDefaultAsync(r => r.ResignationId == id);

                if (resignation == null)
                    return NotFound(new { message = "Resignation not found." });

                var worker = resignation.Interview?.Worker;

                // Fetch the category name via the junction table safely
                var workerRole = await _context.WorkerCategories
                    .Where(wc => wc.WorkerId == worker.WorkerId)
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

                bool hasClientReview = await _context.Reviews.AnyAsync(r => r.InterviewId == resignation.InterviewId);
                bool isConfirmed = resignation.Interview?.Status == "Resigned" || hasClientReview;

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
                    isConfirmed = isConfirmed
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

                if (interview.Status == "Resigned")
                    return BadRequest(new { message = "This resignation has already been confirmed." });

                var review = new Review
                {
                    InterviewId = model.InterviewId,
                    Rating = model.Rating,
                    Comment = model.Comment,
                    ReviewerRole = "Client",
                    ReviewDate = DateTime.Now
                };
                _context.Reviews.Add(review);

                interview.Status = "Resigned";

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
                // 1. Locate the Interview via the associated Hiring ID trace
                var interview = await _context.Interviews.FindAsync(request.InterviewId);
                if (interview == null) return NotFound(new { message = "Job record not found." });

                // Add client rating and comment feedback to the review log
                var review = new Review
                {
                    InterviewId = request.InterviewId,
                    Rating = request.Rating,
                    Comment = request.Remarks,
                    ReviewerRole = "Client",
                    ReviewDate = DateTime.Now
                };
                _context.Reviews.Add(review);

                // Keep the historic interview flag state as Terminated
                interview.Status = "Terminated";

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
                await transaction.CommitAsync();

                return Ok(new { message = "Contract terminated successfully." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
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
                        jobType = i.JobType ?? "Full-Time"
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
                        t.InterviewId,
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
        

        [HttpPut("UpdateDutyStatus/{workerId}")]
        public async Task<IActionResult> UpdateDutyStatus(int workerId, [FromBody] bool isAvailable)
        {
            try
            {
                var worker = await _context.Workers.FindAsync(workerId);
                if (worker == null) return NotFound(new { message = "Worker not found." });

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
        public async Task<IActionResult> FinalizeHiringDecision([FromBody] HiringUpdateDto model)
        {
            try
            {
                var hiring = await _context.Hiring.FindAsync(model.HiringId);
                if (hiring == null)
                {
                    return NotFound(new { message = "Hiring context record trace entry missing." });
                }

                // Apply Rule 5 criteria context update for client finalization only
                hiring.HiringDecision = "Accepted";
                hiring.HiringDate = DateTime.Now;

                var interview = await _context.Interviews.FindAsync(hiring.InterviewId);
                if (interview != null)
                {
                    interview.Status = "Finalized";
                }

                await _context.SaveChangesAsync();

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
            public int Rating { get; set; }
        }

        [HttpPost("SubmitWorkerReviewToClient")]
        public async Task<IActionResult> SubmitWorkerReviewToClient([FromBody] Review model)
        {
            try
            {
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
                if (string.IsNullOrEmpty(userIdStr)) return Unauthorized(new { message = "Invalid user session." });

                var interview = await _context.Interviews.FindAsync(model.InterviewId);
                if (interview == null) return NotFound(new { message = "Record not found." });

                var review = new Review
                {
                    InterviewId = model.InterviewId,
                    Rating = model.Rating,
                    Comment = model.Comment,
                    ReviewerRole = "Worker",
                    ReviewDate = DateTime.Now
                };
                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();
                
                return Ok(new { message = "Review submitted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error: " + ex.Message });
            }
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
                        reviewerName = r.Interview!.Worker != null ? r.Interview.Worker.Name : "Anonymous Worker",
                        rating = r.Rating,
                        comment = r.Comment,
                        date = r.ReviewDate != null ? r.ReviewDate.Value.ToString("MMM dd, yyyy") : "N/A"
                    })
                    .OrderByDescending(r => r.date)
                    .ToListAsync();
                    
                double avgRating = reviews.Any() ? Math.Round(reviews.Average(r => (double)(r.rating ?? 0)), 1) : 0.0;
                
                return Ok(new {
                    clientId = client.ClientId,
                    name = client.Name,
                    phone = client.Phone,
                    address = client.Address,
                    picture = client.Picture,
                    rating = avgRating,
                    reviewCount = reviews.Count,
                    reviews = reviews
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching client profile: " + ex.Message });
            }
        }
        [AllowAnonymous]
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
                    .Select(r => new
                    {
                        id = r.ReviewId.ToString(),
                        reviewerName = r.Interview.Worker != null ? r.Interview.Worker.Name : "Anonymous Worker",
                        reviewerImage = r.Interview.Worker != null ? r.Interview.Worker.Picture : "",
                        rating = r.Rating ?? 0,
                        comment = r.Comment ?? "",
                        date = r.ReviewDate.HasValue ? r.ReviewDate.Value.ToString("MMM dd, yyyy") : "N/A"
                    })
                    .OrderByDescending(r => r.id)
                    .ToListAsync();

                double avgRating = workerReviewsOfClient.Any()
                    ? Math.Round(workerReviewsOfClient.Average(r => (double)r.rating), 1)
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
                    reviewCount = workerReviewsOfClient.Count,
                    reviews = workerReviewsOfClient
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
                if (radius < 1 || radius > 50)
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


