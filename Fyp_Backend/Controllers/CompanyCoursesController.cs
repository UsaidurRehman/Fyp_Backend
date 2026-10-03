using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using Fyp_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fyp_Backend.Controllers;

[ApiController]
[Route("api/company")]
[Authorize(Roles = "Company")]
public class CompanyCoursesController : ControllerBase
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
    public CompanyCoursesController(Fyp1Context db) => _db = db;

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard()
    {
        var companyId = GetAuthenticatedUserId();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var company = await _db.Companies.AsNoTracking()
            .Where(c => c.CompanyID == companyId)
            .Select(c => new
            {
                id = c.CompanyID,
                companyName = c.CompanyName,
                email = c.Email,
                phoneNo = c.PhoneNo,
                companyAddress = c.CompanyAddress,
                licenseNumber = c.LicenseNumber,
                picture = c.CompanyPicture,
                totalCourses = _db.Courses.Count(course => course.CompanyID == c.CompanyID && !course.IsDeleted),
                activeCourses = _db.Courses.Count(course => course.CompanyID == c.CompanyID && !course.IsDeleted && course.IsPublished && course.EndDate >= today),
                totalEnrollments = _db.CourseEnrollments.Count(enrollment => enrollment.Course.CompanyID == c.CompanyID),
                uniqueEnrolledWorkers = _db.CourseEnrollments
                    .Where(enrollment => enrollment.Course.CompanyID == c.CompanyID)
                    .Select(enrollment => enrollment.WorkerID)
                    .Distinct()
                    .Count(),
                completedEnrollments = _db.CourseEnrollments.Count(enrollment => enrollment.Course.CompanyID == c.CompanyID && enrollment.Status == "Completed")
            })
            .FirstOrDefaultAsync();

        return company == null
            ? NotFound(new { message = "Company profile not found." })
            : Ok(company);
    }

    [HttpGet("courses")]
    public async Task<IActionResult> List([FromQuery] string scope = "all")
    {
        var companyId = GetAuthenticatedUserId();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var query = _db.Courses.AsNoTracking().Where(x => x.CompanyID == companyId);
        query = scope.ToLowerInvariant() switch
        {
            "active" => query.Where(x => !x.IsDeleted && x.IsPublished && x.EndDate >= today),
            "past" => query.Where(x => !x.IsDeleted && x.EndDate < today),
            "archived" => query.Where(x => x.IsDeleted),
            _ => query.Where(x => !x.IsDeleted)
        };
        var rows = await query.OrderByDescending(x => x.CreatedAtUtc).Select(x => new
        {
            courseId = x.CourseID,
            courseName = x.CourseName,
            x.StartDate,
            x.EndDate,
            x.ClassTimings,
            x.Description,
            x.Syllabus,
            x.IsPublished,
            x.IsDeleted,
            enrollmentCount = x.Enrollments.Count,
            completedCount = x.Enrollments.Count(e => e.Status == "Completed")
        }).ToListAsync();
        return Ok(rows);
    }

    [HttpGet("courses/{courseId:int}")]
    public async Task<IActionResult> Detail(int courseId)
    {
        var companyId = GetAuthenticatedUserId();
        var item = await _db.Courses.AsNoTracking()
            .Where(x => x.CourseID == courseId && x.CompanyID == companyId)
            .Select(x => new
            {
                courseId = x.CourseID,
                courseName = x.CourseName,
                x.StartDate,
                x.EndDate,
                x.ClassTimings,
                x.Description,
                x.Syllabus,
                x.IsPublished,
                x.IsDeleted,
                enrollments = x.Enrollments.OrderByDescending(e => e.EnrolledAtUtc).Select(e => new
                {
                    enrollmentId = e.EnrollmentID,
                    workerId = e.WorkerID,
                    workerName = e.Worker.Name,
                    workerPicture = e.Worker.Picture,
                    e.Status,
                    e.EnrolledAtUtc,
                    e.CompletionDateUtc,
                    certificateId = e.Certificate == null ? (int?)null : e.Certificate.CertificateID,
                    certificateCode = e.Certificate == null ? null : e.Certificate.CertificateCode
                })
            }).FirstOrDefaultAsync();
        return item == null ? NotFound(new { message = "Course not found." }) : Ok(item);
    }

    [HttpPost("courses")]
    public async Task<IActionResult> Create(CreateCourseRequest request)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        if (request.EndDate < request.StartDate)
            return BadRequest(new { message = "End date cannot be earlier than start date." });
        var companyId = GetAuthenticatedUserId();
        if (!await _db.Companies.AnyAsync(x => x.CompanyID == companyId)) return Unauthorized();
        var item = new Course
        {
            CompanyID = companyId,
            CourseName = request.CourseName.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            ClassTimings = request.ClassTimings.Trim(),
            Description = request.Description?.Trim(),
            Syllabus = request.Syllabus?.Trim(),
            IsPublished = request.IsPublished,
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.Courses.Add(item);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Detail), new { courseId = item.CourseID }, new { courseId = item.CourseID, message = "Course created." });
    }

    [HttpPut("courses/{courseId:int}")]
    public async Task<IActionResult> Update(int courseId, CreateCourseRequest request)
    {
        if (request.EndDate < request.StartDate) return BadRequest(new { message = "Invalid date range." });
        var companyId = GetAuthenticatedUserId();
        var item = await _db.Courses.FirstOrDefaultAsync(x => x.CourseID == courseId && x.CompanyID == companyId && !x.IsDeleted);
        if (item == null) return NotFound(new { message = "Course not found." });
        item.CourseName = request.CourseName.Trim(); item.StartDate = request.StartDate; item.EndDate = request.EndDate;
        item.ClassTimings = request.ClassTimings.Trim(); item.Description = request.Description?.Trim();
        item.Syllabus = request.Syllabus?.Trim(); item.IsPublished = request.IsPublished; item.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Course updated." });
    }

    [HttpDelete("courses/{courseId:int}")]
    public async Task<IActionResult> Delete(int courseId)
    {
        var companyId = GetAuthenticatedUserId();
        var item = await _db.Courses.Include(x => x.Enrollments).FirstOrDefaultAsync(x => x.CourseID == courseId && x.CompanyID == companyId);
        if (item == null) return NotFound(new { message = "Course not found." });
        if (item.Enrollments.Count == 0)
        {
            _db.Courses.Remove(item); await _db.SaveChangesAsync();
            return Ok(new { deletionType = "Deleted", message = "Course permanently deleted." });
        }
        item.IsDeleted = true; item.IsPublished = false; item.DeletedAtUtc = DateTime.UtcNow; item.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { deletionType = "Archived", message = "Course archived because it has enrollment history." });
    }

    [HttpPost("enrollments/{enrollmentId:int}/complete")]
    public async Task<IActionResult> Complete(int enrollmentId)
    {
        var companyId = GetAuthenticatedUserId();
        await using var tx = await _db.Database.BeginTransactionAsync();
        var enrollment = await _db.CourseEnrollments.Include(x => x.Course).Include(x => x.Certificate)
            .FirstOrDefaultAsync(x => x.EnrollmentID == enrollmentId && x.Course.CompanyID == companyId);
        if (enrollment == null) return NotFound(new { message = "Enrollment not found." });
        if (enrollment.Status == "Withdrawn") return BadRequest(new { message = "A withdrawn enrollment cannot be completed." });
        if (enrollment.Certificate != null)
            return Ok(new { message = "Already completed.", certificateId = enrollment.Certificate.CertificateID, certificateCode = enrollment.Certificate.CertificateCode });

        enrollment.Status = "Completed"; enrollment.CompletionDateUtc = DateTime.UtcNow; enrollment.UpdatedAtUtc = DateTime.UtcNow;
        var code = await UniqueCode();
        var certificate = new CourseCertificate
        {
            EnrollmentID = enrollment.EnrollmentID,
            CourseID = enrollment.CourseID,
            WorkerID = enrollment.WorkerID,
            CompanyID = companyId,
            CertificateCode = code,
            IssuedDateUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.CourseCertificates.Add(certificate);
        await _db.SaveChangesAsync(); await tx.CommitAsync();
        return Ok(new { message = "Worker completed the course and certificate was issued.", certificateId = certificate.CertificateID, certificateCode = code });
    }

    private async Task<string> UniqueCode()
    {
        string code;
        do { code = "FYP-COURSE-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(6)); }
        while (await _db.CourseCertificates.AnyAsync(x => x.CertificateCode == code));
        return code;
    }
}

public class CreateCourseRequest
{
    [Required, MaxLength(150)] public string CourseName { get; set; } = "";
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    [Required, MaxLength(200)] public string ClassTimings { get; set; } = "";
    public string? Description { get; set; }
    public string? Syllabus { get; set; }
    public bool IsPublished { get; set; } = true;
}
