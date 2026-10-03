using System.Security.Claims;
using Fyp_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fyp_Backend.Controllers;

[ApiController]
[Route("api/worker")]
[Authorize(Roles = "Worker")]
public class WorkerCoursesController : ControllerBase
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
    public WorkerCoursesController(Fyp1Context db) => _db = db;

    [HttpGet("courses")]
    public async Task<IActionResult> Browse([FromQuery] string? search = null, [FromQuery] int? companyId = null)
    {
        var workerId = GetAuthenticatedUserId();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var q = _db.Courses.AsNoTracking().Where(x => !x.IsDeleted && x.IsPublished && x.EndDate >= today);
        if (companyId.HasValue) q = q.Where(x => x.CompanyID == companyId);
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(x => x.CourseName.Contains(search) || (x.Description != null && x.Description.Contains(search)));
        return Ok(await q.OrderBy(x => x.StartDate).Select(x => new
        {
            courseId = x.CourseID,
            courseName = x.CourseName,
            companyId = x.CompanyID,
            companyName = x.Company.CompanyName,
            companyPicture = x.Company.CompanyPicture,
            x.StartDate,
            x.EndDate,
            x.ClassTimings,
            x.Description,
            x.Syllabus,
            enrollmentStatus = x.Enrollments.Where(e => e.WorkerID == workerId).Select(e => e.Status).FirstOrDefault()
        }).ToListAsync());
    }

    [HttpPost("courses/{courseId:int}/enroll")]
    public async Task<IActionResult> Enroll(int courseId)
    {
        var workerId = GetAuthenticatedUserId();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var course = await _db.Courses.FirstOrDefaultAsync(x => x.CourseID == courseId && !x.IsDeleted && x.IsPublished);
        if (course == null) return NotFound(new { message = "Course is not available." });
        if (course.EndDate < today) return BadRequest(new { message = "This course has ended." });
        var existing = await _db.CourseEnrollments.FirstOrDefaultAsync(x => x.CourseID == courseId && x.WorkerID == workerId);
        if (existing != null) return Conflict(new { message = "You are already enrolled in this course.", enrollmentId = existing.EnrollmentID, status = existing.Status });
        var enrollment = new CourseEnrollment { CourseID = courseId, WorkerID = workerId, Status = "Enrolled", EnrolledAtUtc = DateTime.UtcNow };
        _db.CourseEnrollments.Add(enrollment); await _db.SaveChangesAsync();
        return Ok(new { message = "Enrollment successful.", enrollmentId = enrollment.EnrollmentID });
    }

    [HttpGet("enrollments")]
    public async Task<IActionResult> MyEnrollments([FromQuery] string? status = null)
    {
        var workerId = GetAuthenticatedUserId();
        var q = _db.CourseEnrollments.AsNoTracking().Where(x => x.WorkerID == workerId);
        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(x => x.Status == status);
        return Ok(await q.OrderByDescending(x => x.EnrolledAtUtc).Select(x => new
        {
            enrollmentId = x.EnrollmentID,
            x.Status,
            x.EnrolledAtUtc,
            x.CompletionDateUtc,
            courseId = x.CourseID,
            courseName = x.Course.CourseName,
            companyName = x.Course.Company.CompanyName,
            x.Course.StartDate,
            x.Course.EndDate,
            x.Course.ClassTimings,
            certificateId = x.Certificate == null ? (int?)null : x.Certificate.CertificateID,
            certificateCode = x.Certificate == null ? null : x.Certificate.CertificateCode
        }).ToListAsync());
    }

    [HttpPost("enrollments/{enrollmentId:int}/withdraw")]
    public async Task<IActionResult> Withdraw(int enrollmentId)
    {
        var workerId = GetAuthenticatedUserId();
        var item = await _db.CourseEnrollments.FirstOrDefaultAsync(x => x.EnrollmentID == enrollmentId && x.WorkerID == workerId);
        if (item == null) return NotFound(new { message = "Enrollment not found." });
        if (item.Status == "Completed") return BadRequest(new { message = "A completed course cannot be withdrawn." });
        item.Status = "Withdrawn"; item.WithdrawnAtUtc = DateTime.UtcNow; item.CompletionDateUtc = null; item.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(); return Ok(new { message = "Enrollment withdrawn." });
    }
}
