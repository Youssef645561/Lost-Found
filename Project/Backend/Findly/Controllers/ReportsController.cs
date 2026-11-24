using System.Collections.Generic;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using LostFound.BLL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Findly.BLL.Enums;

namespace LostFound.API.Controllers
{
    [ApiController]
    [Route("api/reports")]
    public class ReportsController : ControllerBase
    {
        private int? _GetCurrentUserID()
        {
            string? claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claimValue, out int id) ? id : null;
        }

        private enUserRole _GetCurrentUserRole()
        {
            string? roleValue = User.FindFirstValue(ClaimTypes.Role);
            return System.Enum.TryParse<enUserRole>(roleValue, out enUserRole role) ? role : enUserRole.User;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateReport([FromBody] clsReport report)
        {
            if (report == null)
                return BadRequest();

            int? currentUserID = this._GetCurrentUserID();
            if (!currentUserID.HasValue)
                return Unauthorized();

            report.UserID = currentUserID.Value;
            report.Status = enReportStatus.Open;

            bool IsSaved = await report.Save();
            if (!IsSaved)
                return BadRequest(new { message = "Could not create report." });

            return CreatedAtAction(nameof(GetReport), new { id = report.ReportID }, report);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllReports(
            enReportType? type,
            enReportStatus? status,
            int? categoryID,
            int? locationID,
            string? searchText,
            int pageNumber = 1,
            int pageSize = 10)
        {
            if (pageNumber < 1)
                pageNumber = 1;
            if (pageSize < 1)
                pageSize = 10;

            List<clsReport> lstReports = await clsReport.GetAll(type, status, categoryID, locationID, searchText, pageNumber, pageSize);
            return Ok(lstReports);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetReport(int id)
        {
            clsReport? report = await clsReport.Find(id);
            if (report == null)
                return NotFound();

            return Ok(report);
        }

        [HttpGet("my")]
        [Authorize]
        public async Task<IActionResult> GetMyReports()
        {
            int? currentUserID = this._GetCurrentUserID();
            if (!currentUserID.HasValue)
                return Unauthorized();

            List<clsReport> lstReports = await clsReport.GetByUserID(currentUserID.Value);
            return Ok(lstReports);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateReport(int id, [FromBody] clsReport report)
        {
            if (report == null)
                return BadRequest();

            int? currentUserID = this._GetCurrentUserID();
            if (!currentUserID.HasValue)
                return Unauthorized();

            clsReport? existingReport = await clsReport.Find(id);
            if (existingReport == null)
                return NotFound();

            enUserRole currentRole = this._GetCurrentUserRole();
            if (currentRole != enUserRole.Admin && existingReport.UserID != currentUserID.Value)
                return Forbid();

            existingReport.Title = report.Title;
            existingReport.Description = report.Description;
            existingReport.ReportDate = report.ReportDate;
            existingReport.CategoryID = report.CategoryID;
            existingReport.LocationID = report.LocationID;

            bool IsSaved = await existingReport.Save();
            if (!IsSaved)
                return BadRequest(new { message = "Could not update report." });

            return Ok(existingReport);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteReport(int id)
        {
            int? currentUserID = this._GetCurrentUserID();
            if (!currentUserID.HasValue)
                return Unauthorized();

            clsReport? existingReport = await clsReport.Find(id);
            if (existingReport == null)
                return NotFound();

            enUserRole currentRole = this._GetCurrentUserRole();
            if (currentRole != enUserRole.Admin && existingReport.UserID != currentUserID.Value)
                return Forbid();

            bool IsDeleted = await existingReport.Delete(currentUserID.Value, currentRole);
            if (!IsDeleted)
                return BadRequest(new { message = "Could not delete report." });

            return NoContent();
        }

        [HttpPatch("{id}/status")]
        [Authorize]
        public async Task<IActionResult> ChangeStatus(int id, enReportStatus newStatus)
        {
            int? currentUserID = this._GetCurrentUserID();
            if (!currentUserID.HasValue)
                return Unauthorized();

            clsReport? existingReport = await clsReport.Find(id);
            if (existingReport == null)
                return NotFound();

            enUserRole currentRole = this._GetCurrentUserRole();
            bool IsChanged = await existingReport.ChangeStatus(newStatus, currentUserID.Value, currentRole);
            if (!IsChanged)
                return Forbid();

            return Ok(existingReport);
        }

        private const long MaxPhotoBytes = 5 * 1024 * 1024; // 5 MB

        // Detect the real image type from the file signature (never trust the client's Content-Type).
        private static string? _DetectImageType(byte[] b)
        {
            if (b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return "image/jpeg";
            if (b.Length >= 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47) return "image/png";
            if (b.Length >= 6 && b[0] == 0x47 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x38) return "image/gif";
            if (b.Length >= 12 && b[0] == 0x52 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x46 && b[8] == 0x57 && b[9] == 0x45 && b[10] == 0x42 && b[11] == 0x50) return "image/webp";
            return null;
        }

        [HttpGet("{id}/photo")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPhoto(int id)
        {
            var photo = await clsReport.GetPhoto(id);
            if (photo == null)
                return NotFound();

            string etag = "\"" + System.Convert.ToHexString(SHA256.HashData(photo.Value.Data))[..16] + "\"";
            Response.Headers.CacheControl = "no-cache"; // always revalidate; 304 when unchanged
            return File(photo.Value.Data, photo.Value.ContentType, null, new EntityTagHeaderValue(etag));
        }

        [HttpPut("{id}/photo")]
        [Authorize]
        [RequestSizeLimit(MaxPhotoBytes + 512 * 1024)]
        public async Task<IActionResult> UploadPhoto(int id, IFormFile? photo)
        {
            int? currentUserID = this._GetCurrentUserID();
            if (!currentUserID.HasValue)
                return Unauthorized();

            clsReport? existingReport = await clsReport.Find(id);
            if (existingReport == null)
                return NotFound();

            if (this._GetCurrentUserRole() != enUserRole.Admin && existingReport.UserID != currentUserID.Value)
                return Forbid();

            if (photo == null || photo.Length == 0)
                return BadRequest(new { message = "No photo was uploaded (form field name must be 'photo')." });
            if (photo.Length > MaxPhotoBytes)
                return BadRequest(new { message = "Photo is too large. Maximum size is 5 MB." });

            byte[] data;
            using (var ms = new System.IO.MemoryStream())
            {
                await photo.CopyToAsync(ms);
                data = ms.ToArray();
            }

            string? contentType = _DetectImageType(data);
            if (contentType == null)
                return BadRequest(new { message = "Unsupported file. Please upload a JPG, PNG, GIF or WEBP image." });

            if (!await existingReport.SetPhoto(data, contentType))
                return BadRequest(new { message = "Could not save photo." });

            return Ok(existingReport);
        }

        [HttpDelete("{id}/photo")]
        [Authorize]
        public async Task<IActionResult> DeletePhoto(int id)
        {
            int? currentUserID = this._GetCurrentUserID();
            if (!currentUserID.HasValue)
                return Unauthorized();

            clsReport? existingReport = await clsReport.Find(id);
            if (existingReport == null)
                return NotFound();

            if (this._GetCurrentUserRole() != enUserRole.Admin && existingReport.UserID != currentUserID.Value)
                return Forbid();

            if (!await existingReport.RemovePhoto())
                return BadRequest(new { message = "Could not remove photo." });

            return NoContent();
        }

        [HttpGet("{id}/matches")]
        [AllowAnonymous]
        public async Task<IActionResult> GetMatches(int id)
        {
            clsReport? existingReport = await clsReport.Find(id);
            if (existingReport == null)
                return NotFound();

            List<clsReport> lstMatches = await existingReport.GetMatches();
            return Ok(lstMatches);
        }
    }
}
