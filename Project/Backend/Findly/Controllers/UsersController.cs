using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LostFound.BLL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Findly.BLL.Enums;

namespace LostFound.API.Controllers
{
    public record ChangePasswordRequest(string? CurrentPassword, string? NewPassword);

    [ApiController]
    [Route("api/users")]
    [Authorize]
    public class UsersController : ControllerBase
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

        private static object _SanitizeUser(clsUser user)
        {
            return new
            {
                user.UserID,
                user.Username,
                user.Email,
                user.Phone,
                Role = user.Role.ToString(),
                user.CreatedDate
            };
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            int? currentUserID = this._GetCurrentUserID();
            if (!currentUserID.HasValue)
                return Unauthorized();

            if (!this._IsAdmin() && currentUserID.Value != id)
                return Forbid();

            clsUser? user = await clsUser.Find(id);
            if (user == null)
                return NotFound();

            return Ok(_SanitizeUser(user));
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsers()
        {
            List<clsUser> lstUsers = await clsUser.GetAll();
            var result = lstUsers.Select(_SanitizeUser).ToList();
            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] clsUser user)
        {
            if (user == null)
                return BadRequest();

            int? currentUserID = this._GetCurrentUserID();
            if (!currentUserID.HasValue)
                return Unauthorized();

            bool isAdmin = this._IsAdmin();
            if (!isAdmin && currentUserID.Value != id)
                return Forbid();

            clsUser? existingUser = await clsUser.Find(id);
            if (existingUser == null)
                return NotFound();

            existingUser.Username = user.Username;
            existingUser.Email = user.Email;
            existingUser.Phone = user.Phone;

            if (isAdmin && user.Role != 0)
                existingUser.Role = user.Role;

            bool IsUpdated = await existingUser.Update();
            if (!IsUpdated)
                return BadRequest(new { message = "Could not update user." });

            return Ok(_SanitizeUser(existingUser));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            clsUser? existingUser = await clsUser.Find(id);
            if (existingUser == null)
                return NotFound();

            bool IsDeleted = await existingUser.Delete();
            if (!IsDeleted)
                return BadRequest(new { message = "Could not delete user." });

            return NoContent();
        }

        [HttpPut("{id}/password")]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordRequest request)
        {
            string currentPassword = request.CurrentPassword ?? string.Empty, newPassword = request.NewPassword ?? string.Empty;
            int? currentUserID = this._GetCurrentUserID();
            if (!currentUserID.HasValue)
                return Unauthorized();

            if (!this._IsAdmin() && currentUserID.Value != id)
                return Forbid();

            if (string.IsNullOrWhiteSpace(newPassword))
                return BadRequest(new { message = "New password is required." });

            clsUser? existingUser = await clsUser.Find(id);
            if (existingUser == null)
                return NotFound();

            bool IsChanged = await existingUser.ChangePassword(currentPassword, newPassword);
            if (!IsChanged)
                return BadRequest(new { message = "Could not change password. Check current password." });

            return Ok(new { message = "Password updated successfully." });
        }
    }
}
