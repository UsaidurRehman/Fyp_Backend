using System.Security.Claims;
using Fyp_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fyp_Backend.Controllers;

[ApiController]
[Route("api/course-certificates")]
public class CourseCertificatesController : ControllerBase
{
    private int GetAuthenticatedUserId()
    {
        var rawId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                 ?? User.FindFirstValue("sub");
        if (!int.TryParse(rawId, out var userId) || userId <= 0)
            throw new UnauthorizedAccessException("The authenticated user ID is invalid.");
        return userId;
    }

    private readonly Fyp1Context _db;
    public CourseCertificatesController(Fyp1Context db) => _db = db;

    [AllowAnonymous]
    [HttpGet("verify/{code}")]
    public async Task<IActionResult> Verify(string code)
    {
        var c = await Projection().FirstOrDefaultAsync(x => x.CertificateCode == code);
        return c == null ? NotFound(new { isValid = false, message = "Certificate not found." }) : Ok(c);
    }

    [Authorize]
    [HttpGet("{certificateId:int}")]
    public async Task<IActionResult> Detail(int certificateId)
    {
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        var id = GetAuthenticatedUserId();
        var q = Projection().Where(x => x.CertificateID == certificateId);
        q = role == "Worker" ? q.Where(x => x.WorkerID == id) : role == "Company" ? q.Where(x => x.CompanyID == id) : q.Where(x => false);
        var c = await q.FirstOrDefaultAsync();
        return c == null ? NotFound(new { message = "Certificate not found." }) : Ok(c);
    }

    private IQueryable<CertificateView> Projection() => _db.CourseCertificates.AsNoTracking().Select(c => new CertificateView
    {
        CertificateID = c.CertificateID,
        CertificateCode = c.CertificateCode,
        WorkerID = c.WorkerID,
        WorkerName = c.Worker.Name ?? "Worker",
        CompanyID = c.CompanyID,
        CompanyName = c.Company.CompanyName,
        CourseName = c.Course.CourseName,
        StartDate = c.Course.StartDate,
        EndDate = c.Course.EndDate,
        CompletionDateUtc = c.Enrollment.CompletionDateUtc,
        IssuedDateUtc = c.IssuedDateUtc,
        IsRevoked = c.IsRevoked,
        IsValid = !c.IsRevoked
    });

    public class CertificateView
    {
        public int CertificateID { get; set; }
        public string CertificateCode { get; set; } = "";
        public int WorkerID { get; set; }
        public string WorkerName { get; set; } = "";
        public int CompanyID { get; set; }
        public string CompanyName { get; set; } = "";
        public string CourseName { get; set; } = "";
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public DateTime? CompletionDateUtc { get; set; }
        public DateTime IssuedDateUtc { get; set; }
        public bool IsRevoked { get; set; }
        public bool IsValid { get; set; }
    }
}
