using Fyp_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Fyp_Backend.Controllers
{
    // Police-issued **Character Certificate** feature (replaces the old FIR flow).
    //
    //  GET  api/Police/GetWorkersForVerification?searchCnic=   → all workers + isCertified
    //  GET  api/Police/GetWorkerDetails/{workerId}            → worker + existing certificate (for renew)
    //  POST api/Police/IssueCharacterCertificate             → issue / renew (police only)
    //  GET  api/Police/GetWorkerCharacterCertificate/{id}     → the current certificate (client + worker view)
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // every police endpoint needs a valid token
    public class PoliceController : ControllerBase
    {
        private readonly Fyp1Context _context;
        private const int CERT_VALIDITY_YEARS = 5;

        public PoliceController(Fyp1Context context)
        {
            _context = context;
        }

        // A certificate counts as "certified" when it exists, is not revoked,
        // and has not passed its 5-year expiry.
        private static bool IsValid(PoliceRecords? c) =>
            c != null && !c.IsRevoked && c.ExpiryDate > DateTime.Now;

        private int GetUserId()
        {
            var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
            return int.TryParse(idStr, out var id) ? id : 0;
        }

        // ── List all workers with their certification status (drives the two tabs) ──
        [HttpGet("GetWorkersForVerification")]
        public async Task<IActionResult> GetWorkersForVerification([FromQuery] string searchCnic = "")
        {
            try
            {
                var query = _context.Workers.AsQueryable();

                if (!string.IsNullOrWhiteSpace(searchCnic))
                    query = query.Where(w => w.Cnic != null && w.Cnic.Contains(searchCnic));

                var workers = await query
                    .Select(w => new
                    {
                        w.WorkerId,
                        Name = w.Name ?? "",
                        Cnic = w.Cnic ?? "",
                        w.Picture,
                        Category = _context.WorkerCategories
                            .Where(wc => wc.WorkerId == w.WorkerId)
                            .Join(_context.Categories, wc => wc.CategoryId, c => c.CategoryId, (wc, c) => c.CategoryName)
                            .FirstOrDefault(),
                        Cert = _context.PoliceRecords
                            .Where(pr => pr.WorkerID == w.WorkerId && !pr.IsRevoked)
                            .OrderByDescending(pr => pr.IssuedDate)
                            .Select(pr => new { pr.ExpiryDate, pr.IssuedDate })
                            .FirstOrDefault()
                    })
                    .ToListAsync();

                var list = workers.Select(w =>
                {
                    bool isCertified = w.Cert != null && w.Cert.ExpiryDate > DateTime.Now;
                    return new
                    {
                        Id = w.WorkerId,
                        w.Name,
                        Category = string.IsNullOrWhiteSpace(w.Category) ? "Domestic Worker" : w.Category,
                        w.Cnic,
                        w.Picture,
                        isCertified,
                        certExpiry = w.Cert != null ? w.Cert.ExpiryDate.ToString("dd-MM-yyyy") : null
                    };
                }).ToList();

                return Ok(new
                {
                    totalResults = list.Count,
                    certifiedCount = list.Count(x => x.isCertified),
                    uncertifiedCount = list.Count(x => !x.isCertified),
                    workers = list
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Server error retrieving workers." });
            }
        }

        // ── One worker + any existing certificate (so the officer can renew) ──
        [HttpGet("GetWorkerDetails/{workerId}")]
        public async Task<IActionResult> GetWorkerDetails(int workerId)
        {
            try
            {
                var worker = await _context.Workers
                    .Where(w => w.WorkerId == workerId)
                    .Select(w => new { Id = w.WorkerId, Name = w.Name ?? "", Cnic = w.Cnic ?? "", w.Picture })
                    .FirstOrDefaultAsync();

                if (worker == null)
                    return NotFound(new { message = "Worker not found." });

                var cert = await _context.PoliceRecords
                    .Where(pr => pr.WorkerID == workerId && !pr.IsRevoked)
                    .OrderByDescending(pr => pr.IssuedDate)
                    .FirstOrDefaultAsync();

                return Ok(new
                {
                    worker.Id,
                    worker.Name,
                    worker.Cnic,
                    worker.Picture,
                    hasCertificate = IsValid(cert),
                    existingCertificate = cert == null ? null : new
                    {
                        cert.CertificateNo,
                        cert.CharacterStatus,
                        cert.Remarks,
                        issuedDate = cert.IssuedDate.ToString("dd-MM-yyyy"),
                        expiryDate = cert.ExpiryDate.ToString("dd-MM-yyyy"),
                        isValid = IsValid(cert)
                    }
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error fetching worker profile." });
            }
        }

        // ── Issue OR renew a character certificate (one active row per worker) ──
        [HttpPost("IssueCharacterCertificate")]
        public async Task<IActionResult> IssueCharacterCertificate([FromBody] IssueCertificateDto model)
        {
            if (model == null || model.WorkerId <= 0)
                return BadRequest(new { message = "A valid Worker ID is required." });

            if (string.IsNullOrWhiteSpace(model.CharacterStatus))
                return BadRequest(new { message = "Character status is required." });

            if (!model.CnicVerified)
                return BadRequest(new { message = "Please confirm you have verified the worker's CNIC." });

            try
            {
                var worker = await _context.Workers.FindAsync(model.WorkerId);
                if (worker == null)
                    return BadRequest(new { message = $"Worker with ID {model.WorkerId} does not exist." });

                // Officer identity comes from the JWT, never the request body.
                // PoliceID is a NOT NULL foreign key in the DB, so we MUST have a
                // valid, existing officer — otherwise fail clearly instead of
                // attempting a null/invalid insert that throws deep in EF.
                int policeId = GetUserId();
                var officer = policeId > 0 ? await _context.PoliceOfficers.FindAsync(policeId) : null;
                if (officer == null)
                    return Unauthorized(new { message = "Your police session is invalid or expired. Please log in again." });

                var now = DateTime.Now;
                var expiry = now.AddYears(CERT_VALIDITY_YEARS);
                string certNo = string.IsNullOrWhiteSpace(model.CertificateNo)
                    ? $"CC-{worker.WorkerId}-{now:yyyyMMdd}"
                    : model.CertificateNo.Trim();

                // One active certificate per worker → renew the existing row if present.
                var cert = await _context.PoliceRecords
                    .FirstOrDefaultAsync(pr => pr.WorkerID == model.WorkerId);

                bool isRenewal = cert != null;
                cert ??= new PoliceRecords { WorkerID = model.WorkerId };

                cert.PoliceID = officer.PoliceID;   // guaranteed valid (checked above)
                cert.CertificateNo = certNo;
                cert.CharacterStatus = model.CharacterStatus.Trim();
                cert.Remarks = model.Remarks?.Trim() ?? string.Empty;
                cert.VerifiedCnic = string.IsNullOrWhiteSpace(model.VerifiedCnic) ? worker.Cnic : model.VerifiedCnic.Trim();
                cert.CnicVerified = model.CnicVerified;
                cert.IssuingStation = officer?.StationName;
                cert.IssuingBadge = officer?.BadgeID;
                cert.IssuedDate = now;
                cert.ExpiryDate = expiry;
                cert.IsRevoked = false;

                if (!isRenewal) _context.PoliceRecords.Add(cert);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    status = "Success",
                    message = isRenewal
                        ? "Character certificate renewed successfully."
                        : "Character certificate issued successfully.",
                    certificateNo = cert.CertificateNo,
                    expiryDate = expiry.ToString("dd-MM-yyyy")
                });
            }
            catch (Exception ex)
            {
                // Surface the underlying DB error (e.g. NOT NULL / constraint failures)
                // so issues like leftover old columns are diagnosable instead of hidden.
                var detail = ex.InnerException?.Message ?? ex.Message;
                Console.WriteLine($"[IssueCharacterCertificate] {detail}");
                return StatusCode(500, new
                {
                    message = "Could not issue the certificate. Please try again.",
                    detail
                });
            }
        }

        // ── The certificate as seen by the CLIENT (background-check button) and the WORKER (own tab) ──
        [HttpGet("GetWorkerCharacterCertificate/{workerId}")]
        public async Task<IActionResult> GetWorkerCharacterCertificate(int workerId)
        {
            try
            {
                var worker = await _context.Workers
                    .Where(w => w.WorkerId == workerId)
                    .Select(w => new { w.WorkerId, w.Name, w.Cnic, w.Picture })
                    .FirstOrDefaultAsync();

                if (worker == null)
                    return NotFound(new { message = "Worker not found." });

                var cert = await _context.PoliceRecords
                    .Where(pr => pr.WorkerID == workerId && !pr.IsRevoked)
                    .OrderByDescending(pr => pr.IssuedDate)
                    .FirstOrDefaultAsync();

                bool valid = IsValid(cert);

                return Ok(new
                {
                    workerId = worker.WorkerId,
                    workerName = worker.Name,
                    workerCnic = worker.Cnic,
                    workerPicture = worker.Picture,
                    isCertified = valid,
                    // status helps the UI: "Verified" | "Expired" | "NotIssued"
                    status = valid ? "Verified" : (cert != null ? "Expired" : "NotIssued"),
                    certificate = cert == null ? null : new
                    {
                        certificateNo = cert.CertificateNo,
                        characterStatus = cert.CharacterStatus,
                        remarks = cert.Remarks,
                        issuingStation = cert.IssuingStation,
                        issuingBadge = cert.IssuingBadge,
                        issuedDate = cert.IssuedDate.ToString("dd-MM-yyyy"),
                        expiryDate = cert.ExpiryDate.ToString("dd-MM-yyyy"),
                        isValid = valid
                    }
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Error fetching character certificate." });
            }
        }

        public class IssueCertificateDto
        {
            public int WorkerId { get; set; }
            public string? CertificateNo { get; set; }
            public string CharacterStatus { get; set; } = string.Empty;
            public string? Remarks { get; set; }
            public string? VerifiedCnic { get; set; }
            public bool CnicVerified { get; set; }
        }
    }
}
