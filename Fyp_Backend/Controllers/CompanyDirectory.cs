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
using Microsoft.EntityFrameworkCore;
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

        [HttpPost("IssueCertificate")]
        public async Task<IActionResult> IssueCertificate([FromBody] IssueCertificateDto dto)
        {
            try
            {
                if (dto == null || dto.WorkerId <= 0 || dto.CompanyId <= 0)
                    return BadRequest(new { message = $"Invalid parameters: WorkerId={dto?.WorkerId}, CompanyId={dto?.CompanyId}" });

                if (string.IsNullOrWhiteSpace(dto.CertificateTitle))
                    return BadRequest(new { message = "Certificate title is required." });

                var certification = new WorkerCertification
                {
                    WorkerID = dto.WorkerId,
                    CompanyID = dto.CompanyId,
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
                        companyName = _context.Companies.Where(c => c.CompanyID == wc.CompanyID).Select(c => c.CompanyName).FirstOrDefault() ?? "Proton Services Ltd."
                    })
                    .FirstOrDefaultAsync();

                if (certificate == null)
                    return NotFound(new { message = "No certificate record found for this worker." });

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
    }

    public class IssueCertificateDto
    {
        public int WorkerId { get; set; }
        public int CompanyId { get; set; }
        public string CertificateTitle { get; set; }
        public string TrainingEvaluationNotes { get; set; }
    }
}