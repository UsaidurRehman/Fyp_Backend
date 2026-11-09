//using Fyp_Backend.Models;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using System;
//using System.ComponentModel;
//using System.Linq;
//using System.Net;
//using System.Threading.Tasks;
//using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
//using static System.Runtime.InteropServices.JavaScript.JSType;

//namespace Fyp_Backend.Controllers
//{
//    [Route("api/[controller]")]
//    [ApiController]
//    public class CompanyDirectoryController : ControllerBase
//    {
//        private readonly Fyp1Context _context;

//        public CompanyDirectoryController(Fyp1Context context)
//        {
//            _context = context;
//        }

//        [HttpGet("GetCompanyProfile")]
//        public async Task<IActionResult> GetCompanyProfile([FromQuery] int companyId)
//        {
//            try
//            {
//                var company = await _context.Companies.FindAsync(companyId);
//                if (company == null)
//                    return NotFound(new { message = "Company not found." });

//                string baseUrl = $"{Request.Scheme}://{Request.Host}";

//                // Formats relative path stored in DB into a complete, fetchable URL
//                string pictureUrl = !string.IsNullOrEmpty(company.CompanyPicture)
//                    ? (company.CompanyPicture.StartsWith("http")
//                        ? company.CompanyPicture
//                        : $"{baseUrl}{(company.CompanyPicture.StartsWith("/") ? "" : "/")}{company.CompanyPicture}")
//                    : $"{baseUrl}/Images/company_default.jpg";

//                return Ok(new
//                {
//                    companyId = company.CompanyID,
//                    companyName = company.CompanyName,
//                    email = company.Email,
//                    phoneNo = company.PhoneNo,
//                    licenseNumber = company.LicenseNumber,
//                    companyAddress = company.CompanyAddress,
//                    companyPicture = pictureUrl
//                });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new { message = "Error: " + (ex.InnerException?.Message ?? ex.Message) });
//            }
//        }

//        [HttpGet("GetAllWorkers")]
//        public async Task<IActionResult> GetAllWorkers()
//        {
//            try
//            {
//                var workerList = await _context.Workers
//                    .Select(w => new
//                    {
//                        w.WorkerId,
//                        w.Name,
//                        w.Bio,
//                        w.Address,
//                        w.Picture,
//                        w.Salary
//                    })
//                    .ToListAsync();

//                var categoryMap = await _context.WorkerCategories
//                    .Join(
//                        _context.Categories,
//                        wc => wc.CategoryId,
//                        c => c.CategoryId,
//                        (wc, c) => new { wc.WorkerId, c.CategoryName }
//                    )
//                    .ToListAsync();

//                var result = workerList.Select(w =>
//                {
//                    // Ensures image paths point to the /Images/ directory cleanly
//                    string rawPic = string.IsNullOrWhiteSpace(w.Picture) ? "worker_default.jpg" : w.Picture;
//                    string cleanPicPath = rawPic.StartsWith("http")
//                        ? rawPic
//                        : (rawPic.StartsWith("/Images/") || rawPic.StartsWith("Images/")
//                            ? (rawPic.StartsWith("/") ? rawPic : "/" + rawPic)
//                            : $"/Images/{rawPic.TrimStart('/')}");

//                    return new
//                    {
//                        workerId = w.WorkerId,
//                        name = w.Name ?? "N/A",
//                        roleTitle = w.Bio ?? "General Service Professional",
//                        location = w.Address ?? "Rawalpindi",
//                        picture = cleanPicPath,
//                        salary = w.Salary.HasValue ? "Rs." + w.Salary.Value.ToString("F0") : "Rs.25000",
//                        rating = "4.0",
//                        categories = categoryMap
//                            .Where(cm => cm.WorkerId == w.WorkerId)
//                            .Select(cm => cm.CategoryName)
//                            .Distinct()
//                            .ToList()
//                    };
//                });

//                return Ok(result);
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new { message = "Error: " + (ex.InnerException?.Message ?? ex.Message) });
//            }
//        }
//        //[HttpGet("GetAllWorkers")]
//        //public async Task<IActionResult> GetAllWorkers()
//        //{
//        //    try
//        //    {
//        //        string baseUrl = $"{Request.Scheme}://{Request.Host}";

//        //        // Step 1: Fetch raw worker records from DB
//        //        var workerList = await _context.Workers
//        //            .Select(w => new
//        //            {
//        //                w.WorkerId,
//        //                w.Name,
//        //                w.Bio,
//        //                w.Address,
//        //                w.Picture,
//        //                w.Salary
//        //            })
//        //            .ToListAsync();

//        //        // Step 2: Fetch category mapping names
//        //        var categoryMap = await _context.WorkerCategories
//        //            .Join(
//        //                _context.Categories,
//        //                wc => wc.CategoryId,
//        //                c => c.CategoryId,
//        //                (wc, c) => new { wc.WorkerId, c.CategoryName }
//        //            )
//        //            .ToListAsync();

//        //        // Step 3: Combine in memory safely and format full image paths
//        //        var result = workerList.Select(w =>
//        //        {
//        //            string rawPic = w.Picture ?? "worker_default.jpg";
//        //            string fullPicUrl = rawPic.StartsWith("http")
//        //                ? rawPic
//        //                : $"{baseUrl}{(rawPic.StartsWith("/") ? "" : "/")}{rawPic}";

//        //            return new
//        //            {
//        //                workerId = w.WorkerId,
//        //                name = w.Name ?? "N/A",
//        //                roleTitle = w.Bio ?? "General Service Professional",
//        //                location = w.Address ?? "Rawalpindi",
//        //                picture = fullPicUrl,
//        //                salary = w.Salary.HasValue ? "Rs." + w.Salary.Value.ToString("F0") : "Rs.25000",
//        //                rating = "4.0",
//        //                categories = categoryMap
//        //                    .Where(cm => cm.WorkerId == w.WorkerId)
//        //                    .Select(cm => cm.CategoryName)
//        //                    .Distinct()
//        //                    .ToList()
//        //            };
//        //        });

//        //        return Ok(result);
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        return StatusCode(500, new { message = "Error: " + (ex.InnerException?.Message ?? ex.Message) });
//        //    }
//        //}

//        [HttpGet("GetWorkerDetails/{workerId}")]
//        public async Task<IActionResult> GetWorkerDetails([FromRoute] int workerId)
//        {
//            if (workerId <= 0)
//                return BadRequest(new { message = "Invalid worker ID." });

//            try
//            {
//                var workerData = await _context.Workers
//                    .Where(w => w.WorkerId == workerId)
//                    .Select(w => new
//                    {
//                        w.WorkerId,
//                        w.Name,
//                        w.Cnic,
//                        w.Phone,
//                        w.Address,
//                        w.Picture,
//                        CategoryName = _context.WorkerCategories
//                            .Where(wc => wc.WorkerId == w.WorkerId)
//                            .Join(
//                                _context.Categories,
//                                wc => wc.CategoryId,
//                                c => c.CategoryId,
//                                (wc, c) => c.CategoryName
//                            )
//                            .FirstOrDefault()
//                    })
//                    .FirstOrDefaultAsync();

//                if (workerData == null)
//                    return NotFound(new { message = "Worker profile not found." });

//                string displayCategory = !string.IsNullOrWhiteSpace(workerData.CategoryName)
//                    ? $"{workerData.CategoryName.Trim().ToUpper()} SPECIALIST"
//                    : "GENERAL SERVICE SPECIALIST";

//                string rawPic = string.IsNullOrWhiteSpace(workerData.Picture) ? "worker_default.jpg" : workerData.Picture;
//                string cleanPicPath = rawPic.StartsWith("http")
//                    ? rawPic
//                    : (rawPic.StartsWith("/Images/") || rawPic.StartsWith("Images/")
//                        ? (rawPic.StartsWith("/") ? rawPic : "/" + rawPic)
//                        : $"/Images/{rawPic.TrimStart('/')}");

//                return Ok(new
//                {
//                    workerId = workerData.WorkerId,
//                    name = workerData.Name ?? "N/A",
//                    cnic = string.IsNullOrEmpty(workerData.Cnic) ? "37405-XXXXXXX-X" : workerData.Cnic,
//                    phone = string.IsNullOrEmpty(workerData.Phone) ? "+92 300 555 1234" : workerData.Phone,
//                    address = string.IsNullOrEmpty(workerData.Address) ? "Rawalpindi, PK" : workerData.Address,
//                    picture = cleanPicPath,
//                    roleTitle = displayCategory
//                });
//            }
//            catch (Exception)
//            {
//                return StatusCode(500, new { message = "An error occurred while retrieving worker details." });
//            }
//        }
//        [HttpPost("IssueCertificate")]
//        public async Task<IActionResult> IssueCertificate([FromBody] IssueCertificateDto dto)
//        {
//            try
//            {
//                if (dto == null || dto.WorkerId <= 0 || dto.CompanyId <= 0)
//                    return BadRequest(new { message = $"Invalid parameters: WorkerId={dto?.WorkerId}, CompanyId={dto?.CompanyId}" });

//                if (string.IsNullOrWhiteSpace(dto.CertificateTitle))
//                    return BadRequest(new { message = "Certificate title is required." });

//                var certification = new WorkerCertification
//                {
//                    WorkerID = dto.WorkerId,
//                    CompanyID = dto.CompanyId,
//                    CertificateTitle = dto.CertificateTitle.Trim(),
//                    TrainingEvaluationNotes = dto.TrainingEvaluationNotes?.Trim(),
//                    IssuedDate = DateTime.Now
//                };

//                _context.WorkerCertifications.Add(certification);
//                await _context.SaveChangesAsync();

//                return Ok(new { status = "Success", message = "Certificate issued and published successfully!" });
//            }
//            catch (DbUpdateException dbEx)
//            {
//                // Catches SQL foreign key or constraint violations directly
//                var innerError = dbEx.InnerException != null ? dbEx.InnerException.Message : dbEx.Message;
//                return StatusCode(500, new { message = "Database Error: " + innerError });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new { message = "Error: " + (ex.InnerException?.Message ?? ex.Message) });
//            }
//        }

//    }
//    public class IssueCertificateDto
//    {
//        public int WorkerId { get; set; }
//        public int CompanyId { get; set; }
//        public string CertificateTitle { get; set; }
//        public string TrainingEvaluationNotes { get; set; }
//    }

//}
using Fyp_Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Fyp_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CompanyDirectoryController : ControllerBase
    {
        private readonly Fyp1Context _context;

        public CompanyDirectoryController(Fyp1Context context)
        {
            _context = context;
        }

        [HttpGet("GetCompanyProfile")]
        public async Task<IActionResult> GetCompanyProfile([FromQuery] int companyId)
        {
            try
            {
                var company = await _context.Companies.FindAsync(companyId);
                if (company == null)
                    return NotFound(new { message = "Company not found." });

                // Clean relative path formatting matching worker endpoints
                string rawPic = string.IsNullOrWhiteSpace(company.CompanyPicture) ? "company_default.jpg" : company.CompanyPicture;
                string cleanPicPath = rawPic.StartsWith("http")
                    ? rawPic
                    : (rawPic.StartsWith("/Images/") || rawPic.StartsWith("Images/")
                        ? (rawPic.StartsWith("/") ? rawPic : "/" + rawPic)
                        : $"/Images/{rawPic.TrimStart('/')}");

                return Ok(new
                {
                    companyId = company.CompanyID,
                    companyName = company.CompanyName,
                    email = company.Email,
                    phoneNo = company.PhoneNo,
                    licenseNumber = company.LicenseNumber,
                    companyAddress = company.CompanyAddress,
                    companyPicture = cleanPicPath
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error: " + (ex.InnerException?.Message ?? ex.Message) });
            }
        }

        [HttpGet("GetAllWorkers")]
        public async Task<IActionResult> GetAllWorkers()
        {
            try
            {
                var workerList = await _context.Workers
                    .Select(w => new
                    {
                        w.WorkerId,
                        w.Name,
                        w.Bio,
                        w.Address,
                        w.Picture,
                        w.Salary
                    })
                    .ToListAsync();

                var categoryMap = await _context.WorkerCategories
                    .Join(
                        _context.Categories,
                        wc => wc.CategoryId,
                        c => c.CategoryId,
                        (wc, c) => new { wc.WorkerId, c.CategoryName }
                    )
                    .ToListAsync();

                var result = workerList.Select(w =>
                {
                    string rawPic = string.IsNullOrWhiteSpace(w.Picture) ? "worker_default.jpg" : w.Picture;
                    string cleanPicPath = rawPic.StartsWith("http")
                        ? rawPic
                        : (rawPic.StartsWith("/Images/") || rawPic.StartsWith("Images/")
                            ? (rawPic.StartsWith("/") ? rawPic : "/" + rawPic)
                            : $"/Images/{rawPic.TrimStart('/')}");

                    return new
                    {
                        workerId = w.WorkerId,
                        name = w.Name ?? "N/A",
                        roleTitle = w.Bio ?? "General Service Professional",
                        location = w.Address ?? "Rawalpindi",
                        picture = cleanPicPath,
                        salary = w.Salary.HasValue ? "Rs." + w.Salary.Value.ToString("F0") : "Rs.25000",
                        rating = "4.0",
                        categories = categoryMap
                            .Where(cm => cm.WorkerId == w.WorkerId)
                            .Select(cm => cm.CategoryName)
                            .Distinct()
                            .ToList()
                    };
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error: " + (ex.InnerException?.Message ?? ex.Message) });
            }
        }

        [HttpGet("GetWorkerDetails/{workerId}")]
        public async Task<IActionResult> GetWorkerDetails([FromRoute] int workerId)
        {
            if (workerId <= 0)
                return BadRequest(new { message = "Invalid worker ID." });

            try
            {
                var workerData = await _context.Workers
                    .Where(w => w.WorkerId == workerId)
                    .Select(w => new
                    {
                        w.WorkerId,
                        w.Name,
                        w.Cnic,
                        w.Phone,
                        w.Address,
                        w.Picture,
                        CategoryName = _context.WorkerCategories
                            .Where(wc => wc.WorkerId == w.WorkerId)
                            .Join(
                                _context.Categories,
                                wc => wc.CategoryId,
                                c => c.CategoryId,
                                (wc, c) => c.CategoryName
                            )
                            .FirstOrDefault()
                    })
                    .FirstOrDefaultAsync();

                if (workerData == null)
                    return NotFound(new { message = "Worker profile not found." });

                string displayCategory = !string.IsNullOrWhiteSpace(workerData.CategoryName)
                    ? $"{workerData.CategoryName.Trim().ToUpper()} SPECIALIST"
                    : "GENERAL SERVICE SPECIALIST";

                string rawPic = string.IsNullOrWhiteSpace(workerData.Picture) ? "worker_default.jpg" : workerData.Picture;
                string cleanPicPath = rawPic.StartsWith("http")
                    ? rawPic
                    : (rawPic.StartsWith("/Images/") || rawPic.StartsWith("Images/")
                        ? (rawPic.StartsWith("/") ? rawPic : "/" + rawPic)
                        : $"/Images/{rawPic.TrimStart('/')}");

                return Ok(new
                {
                    workerId = workerData.WorkerId,
                    name = workerData.Name ?? "N/A",
                    cnic = string.IsNullOrEmpty(workerData.Cnic) ? "37405-XXXXXXX-X" : workerData.Cnic,
                    phone = string.IsNullOrEmpty(workerData.Phone) ? "+92 300 555 1234" : workerData.Phone,
                    address = string.IsNullOrEmpty(workerData.Address) ? "Rawalpindi, PK" : workerData.Address,
                    picture = cleanPicPath,
                    roleTitle = displayCategory
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving worker details." });
            }
        }

        [Authorize(Roles = "Company")]
        [HttpPost("IssueCertificate")]
        //public async Task<IActionResult> IssueCertificate([FromBody] IssueCertificateDto dto)
        public async Task<IActionResult> IssueCertificate([FromBody] CompanyCertificateRequest dto)
        {
            try
            {
                var rawCompanyId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                                  ?? User.FindFirstValue("sub");
                if (!int.TryParse(rawCompanyId, out var authenticatedCompanyId) || authenticatedCompanyId <= 0)
                    return Unauthorized(new { message = "The authenticated Company ID is invalid." });

                if (dto == null || dto.WorkerId <= 0)
                    return BadRequest(new { message = $"Invalid WorkerId: {dto?.WorkerId}" });

                if (string.IsNullOrWhiteSpace(dto.CertificateTitle))
                    return BadRequest(new { message = "Certificate title is required." });

                var certification = new WorkerCertification
                {
                    WorkerID = dto.WorkerId,
                    CompanyID = authenticatedCompanyId,
                    CertificateTitle = dto.CertificateTitle.Trim(),
                    TrainingEvaluationNotes = dto.TrainingEvaluationNotes?.Trim(),
                    IssuedDate = DateTime.Now
                };

                _context.WorkerCertifications.Add(certification);
                await _context.SaveChangesAsync();

                return Ok(new { status = "Success", message = "Certificate issued and published successfully!" });
            }
            catch (DbUpdateException dbEx)
            {
                var innerError = dbEx.InnerException != null ? dbEx.InnerException.Message : dbEx.Message;
                return StatusCode(500, new { message = "Database Error: " + innerError });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error: " + (ex.InnerException?.Message ?? ex.Message) });
            }
        }
        [HttpGet("GetWorkerCertificates/{workerId}")]
        public async Task<IActionResult> GetWorkerCertificates([FromRoute] int workerId)
        {
            if (workerId <= 0)
                return BadRequest(new { message = "Invalid worker ID." });

            try
            {
                // Keep both certificate sources and include every issued course certificate.
                var companyCertificates = await _context.WorkerCertifications
                    .AsNoTracking()
                    .Where(c => c.WorkerID == workerId)
                    .Select(c => new WorkerCertificateRecordDto
                    {
                        CertificateId = c.CertificationID,
                        CertificateType = "CompanyCertification",
                        CertificateTitle = c.CertificateTitle,
                        EvaluationNotes = c.TrainingEvaluationNotes,
                        IssuedDate = c.IssuedDate,
                        WorkerName = c.Worker.Name,
                        WorkerPicture = c.Worker.Picture,
                        CompanyName = c.Company.CompanyName
                    })
                    .ToListAsync();

                var courseCertificates = await _context.CourseCertificates
                    .AsNoTracking()
                    .Where(c => c.WorkerID == workerId)
                    .Select(c => new WorkerCertificateRecordDto
                    {
                        CertificateId = c.CertificateID,
                        CertificateType = "CourseCompletion",
                        CertificateTitle = c.Course.CourseName,
                        CourseDescription = c.Course.Description,
                        Syllabus = c.Course.Syllabus,
                        IssuedDate = c.IssuedDateUtc,
                        WorkerName = c.Worker.Name,
                        WorkerPicture = c.Worker.Picture,
                        CompanyName = c.Company.CompanyName,
                        CourseStartDate = c.Course.StartDate,
                        CourseEndDate = c.Course.EndDate,
                        CompletionDateUtc = c.Enrollment.CompletionDateUtc,
                        CertificateCode = c.CertificateCode,
                    })
                    .ToListAsync();

                var certificates = companyCertificates
                    .Concat(courseCertificates)
                    .OrderByDescending(c => c.IssuedDate)
                    .Select(c => new
                    {
                        certificateId = c.CertificateId,
                        certificateType = c.CertificateType,
                        certificateTitle = c.CertificateTitle,
                        evaluationNotes = c.EvaluationNotes,
                        courseDescription = c.CourseDescription,
                        syllabus = c.Syllabus,
                        issuedDate = c.IssuedDate.ToString("yyyy-MM-dd"),
                        workerName = c.WorkerName,
                        workerPicture = c.WorkerPicture,
                        companyName = c.CompanyName,
                        courseStartDate = c.CourseStartDate?.ToString("yyyy-MM-dd"),
                        courseEndDate = c.CourseEndDate?.ToString("yyyy-MM-dd"),
                        completionDateUtc = c.CompletionDateUtc?.ToString("yyyy-MM-dd"),
                        certificateCode = c.CertificateCode,
                        certificateUrl = c.CertificateUrl
                    })
                    .ToList();

                return Ok(certificates);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error: " + (ex.InnerException?.Message ?? ex.Message) });
            }
        }

        [HttpGet("GetWorkerCertificateDetail/{workerId}")]
        public async Task<IActionResult> GetWorkerCertificateDetail([FromRoute] int workerId)
        {
            if (workerId <= 0)
                return BadRequest(new { message = "Invalid worker ID." });

            try
            {
                var certificate = await _context.WorkerCertifications
                    .Where(wc => wc.WorkerID == workerId)
                    .OrderByDescending(wc => wc.IssuedDate)
                    .Select(wc => new
                    {
                        certificateId = wc.CertificationID,
                        certificateTitle = wc.CertificateTitle,
                        evaluationNotes = wc.TrainingEvaluationNotes,
                        issuedDate = wc.IssuedDate.ToString("dd-MM-yyyy"),
                        workerName = _context.Workers.Where(w => w.WorkerId == wc.WorkerID).Select(w => w.Name).FirstOrDefault() ?? "N/A",
                        workerPicture = _context.Workers.Where(w => w.WorkerId == wc.WorkerID).Select(w => w.Picture).FirstOrDefault(),
                        companyName = _context.Companies.Where(c => c.CompanyID == wc.CompanyID).Select(c => c.CompanyName).FirstOrDefault()
                    })
                    .FirstOrDefaultAsync();

                // Backward-compatible single-record endpoint. The client certificate
                // flow uses GetWorkerCertificates above to display every record.
                if (certificate == null)
                {
                    var courseCertificate = await _context.CourseCertificates
                        .Where(c => c.WorkerID == workerId)
                        .OrderByDescending(c => c.IssuedDateUtc)
                        .Select(c => new
                        {
                            certificateId = c.CertificateID,
                            certificateTitle = c.Course.CourseName,
                            evaluationNotes = (string?)null,
                            courseDescription = c.Course.Description,
                            issuedDate = c.IssuedDateUtc.ToString("dd-MM-yyyy"),
                            workerName = c.Worker.Name ?? "N/A",
                            workerPicture = c.Worker.Picture,
                            companyName = c.Company.CompanyName,
                            certificateCode = c.CertificateCode,
                        })
                        .FirstOrDefaultAsync();

                    if (courseCertificate == null)
                        return NotFound(new { message = "No certificate record found for this worker." });

                    string courseRawPic = string.IsNullOrWhiteSpace(courseCertificate.workerPicture) ? "worker_default.jpg" : courseCertificate.workerPicture;
                    string courseCleanPicPath = courseRawPic.StartsWith("http")
                        ? courseRawPic
                        : (courseRawPic.StartsWith("/Images/") || courseRawPic.StartsWith("Images/")
                            ? (courseRawPic.StartsWith("/") ? courseRawPic : "/" + courseRawPic)
                            : $"/Images/{courseRawPic.TrimStart('/')}");

                    return Ok(new
                    {
                        courseCertificate.certificateId,
                        courseCertificate.certificateTitle,
                        courseCertificate.evaluationNotes,
                        courseCertificate.courseDescription,
                        courseCertificate.issuedDate,
                        courseCertificate.workerName,
                        workerPicture = courseCleanPicPath,
                        courseCertificate.companyName,
                        courseCertificate.certificateCode,
                        certificateType = "CourseCompletion"
                    });
                }

                string rawPic = string.IsNullOrWhiteSpace(certificate.workerPicture) ? "worker_default.jpg" : certificate.workerPicture;
                string cleanPicPath = rawPic.StartsWith("http")
                    ? rawPic
                    : (rawPic.StartsWith("/Images/") || rawPic.StartsWith("Images/")
                        ? (rawPic.StartsWith("/") ? rawPic : "/" + rawPic)
                        : $"/Images/{rawPic.TrimStart('/')}");

                return Ok(new
                {
                    certificate.certificateId,
                    certificate.certificateTitle,
                    certificate.evaluationNotes,
                    certificate.issuedDate,
                    certificate.workerName,
                    workerPicture = cleanPicPath,
                    certificate.companyName
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error: " + (ex.InnerException?.Message ?? ex.Message) });
            }
        }

        private sealed class WorkerCertificateRecordDto
        {
            public WorkerCertificateRecordDto() { }

            public int CertificateId { get; set; }
            public string CertificateType { get; set; } = string.Empty;
            public string? CertificateTitle { get; set; }
            public string? EvaluationNotes { get; set; }
            public string? CourseDescription { get; set; }
            public string? Syllabus { get; set; }
            public DateTime IssuedDate { get; set; }
            public string? WorkerName { get; set; }
            public string? WorkerPicture { get; set; }
            public string? CompanyName { get; set; }
            public DateOnly? CourseStartDate { get; set; }
            public DateOnly? CourseEndDate { get; set; }
            public DateTime? CompletionDateUtc { get; set; }
            public string? CertificateCode { get; set; }
            public string? CertificateUrl { get; set; }
        }
    }

    //public class IssueCertificateDto
    //{
    //    public int WorkerId { get; set; }
    //    public int CompanyId { get; set; }
    //    public string CertificateTitle { get; set; }
    //    public string TrainingEvaluationNotes { get; set; }
    //}
    public class CompanyCertificateRequest
    {
        public int WorkerId { get; set; }
        public int CompanyId { get; set; }
        public string CertificateTitle { get; set; }
        public string TrainingEvaluationNotes { get; set; }
    }
}