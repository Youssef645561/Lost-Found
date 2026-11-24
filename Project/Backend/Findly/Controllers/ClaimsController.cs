using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using LostFound.BLL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Findly.BLL.Enums;

namespace LostFound.API.Controllers
{
    [ApiController]
    [Route("api/claims")]
    [Authorize]
    public class ClaimsController : ControllerBase
    {
        private int? _GetCurrentUserID()
        {
            string? claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claimValue, out int id) ? id : null;
        }

        private bool _IsAdmin()
        {
            return User.IsInRole(enUserRole.Admin.ToString());
        }

        [HttpPost]
        public async Task<IActionResult> CreateClaim([FromBody] clsClaim claim)
        {
            if (claim == null)
                return BadRequest();

            int? currentUserID = this._GetCurrentUserID();
            if (!currentUserID.HasValue)
                return Unauthorized();

            claim.UserID = currentUserID.Value;
            claim.Status = enClaimStatus.Pending;
            claim.AdminID = null;
            claim.RejectionReason = null;

            bool IsCreated = await claim.Create();
            if (!IsCreated)
                return BadRequest(new { message = "Could not create claim. Check report status, type, or existing claims." });

            return CreatedAtAction(nameof(GetClaim), new { id = claim.ClaimID }, claim);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetClaim(int id)
        {
            int? currentUserID = this._GetCurrentUserID();
            if (!currentUserID.HasValue)
                return Unauthorized();

            clsClaim? claim = await clsClaim.Find(id);
            if (claim == null)
                return NotFound();

            if (!this._IsAdmin() && claim.UserID != currentUserID.Value)
                return Forbid();

            return Ok(claim);
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyClaims()
        {
            int? currentUserID = this._GetCurrentUserID();
            if (!currentUserID.HasValue)
                return Unauthorized();

            List<clsClaim> lstClaims = await clsClaim.GetByUserID(currentUserID.Value);
            return Ok(lstClaims);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllClaims(enClaimStatus? status)
        {
            List<clsClaim> lstClaims = await clsClaim.GetAll(status);
            return Ok(lstClaims);
        }

        [HttpGet("report/{reportID}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetClaimsByReport(int reportID)
        {
            List<clsClaim> lstClaims = await clsClaim.GetByReportID(reportID);
            return Ok(lstClaims);
        }

        [HttpPatch("{id}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveClaim(int id)
        {
            int? adminID = this._GetCurrentUserID();
            if (!adminID.HasValue)
                return Unauthorized();

            clsClaim? claim = await clsClaim.Find(id);
            if (claim == null)
                return NotFound();

            bool IsApproved = await claim.Approve(adminID.Value);
            if (!IsApproved)
                return BadRequest(new { message = "Could not approve claim." });

            return Ok(claim);
        }

        [HttpPatch("{id}/reject")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectClaim(int id, string rejectionReason)
        {
            int? adminID = this._GetCurrentUserID();
            if (!adminID.HasValue)
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(rejectionReason))
                return BadRequest(new { message = "Rejection reason is required." });

            clsClaim? claim = await clsClaim.Find(id);
            if (claim == null)
                return NotFound();

            bool IsRejected = await claim.Reject(adminID.Value, rejectionReason);
            if (!IsRejected)
                return BadRequest(new { message = "Could not reject claim." });

            return Ok(claim);
        }

        [HttpPatch("{id}/cancel")]
        public async Task<IActionResult> CancelClaim(int id)
        {
            int? currentUserID = this._GetCurrentUserID();
            if (!currentUserID.HasValue)
                return Unauthorized();

            clsClaim? claim = await clsClaim.Find(id);
            if (claim == null)
                return NotFound();

            if (claim.UserID != currentUserID.Value)
                return Forbid();

            bool IsCancelled = await claim.Cancel(currentUserID.Value);
            if (!IsCancelled)
                return BadRequest(new { message = "Could not cancel claim. Only pending claims can be cancelled." });

            return Ok(claim);
        }

        [HttpPatch("{id}/reopen")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ReopenClaim(int id)
        {
            int? adminID = this._GetCurrentUserID();
            if (!adminID.HasValue)
                return Unauthorized();

            clsClaim? claim = await clsClaim.Find(id);
            if (claim == null)
                return NotFound();

            bool IsReopened = await claim.Reopen(adminID.Value);
            if (!IsReopened)
                return BadRequest(new { message = "Could not reopen claim." });

            return Ok(claim);
        }

        [HttpGet("{id}/evidence")]
        public async Task<IActionResult> GetEvidence(int id)
        {
            int? currentUserID = this._GetCurrentUserID();
            if (!currentUserID.HasValue)
                return Unauthorized();

            clsClaim? claim = await clsClaim.Find(id);
            if (claim == null)
                return NotFound();

            if (!this._IsAdmin() && claim.UserID != currentUserID.Value)
                return Forbid();

            if (string.IsNullOrWhiteSpace(claim.PhotoPath) || !System.IO.File.Exists(claim.PhotoPath))
                return NotFound(new { message = "Evidence PhotoPath not found." });

            string contentType = "application/octet-stream";
            string ext = Path.GetExtension(claim.PhotoPath).ToLowerInvariant();
            if (ext == ".jpg" || ext == ".jpeg")
                contentType = "image/jpeg";
            else if (ext == ".png")
                contentType = "image/png";
            else if (ext == ".gif")
                contentType = "image/gif";
            else if (ext == ".webp")
                contentType = "image/webp";

            byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(claim.PhotoPath);
            return File(fileBytes, contentType, Path.GetFileName(claim.PhotoPath));
        }
    }
}
