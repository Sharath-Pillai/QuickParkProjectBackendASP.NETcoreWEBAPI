using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickParkAPI.Data;
using QuickParkAPI.DTOs;
using QuickParkAPI.Services;

namespace QuickParkAPI.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _db;

        public UsersController(AppDbContext db) => _db = db;

        private int GetUserId() => ClaimsHelper.GetUserId(User);

        // GET /api/users/profile
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var user = await _db.Users.FindAsync(GetUserId());
            if (user == null) return NotFound(new { error = "User profile not found" });
            return Ok(MappingHelper.ToUserResponse(user));
        }

        // PUT /api/users/profile
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var user = await _db.Users.FindAsync(GetUserId());
            if (user == null) return NotFound(new { error = "User profile not found" });

            if (dto.Name != null) user.Name = dto.Name;
            if (dto.Phone != null) user.Phone = dto.Phone;
            if (dto.Address != null) user.Address = dto.Address;
            if (dto.ProfileImage != null) user.ProfileImage = dto.ProfileImage;
            user.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Ok(new { success = true, user = MappingHelper.ToUserResponse(user) });
        }

        // POST /api/users/kyc
        [HttpPost("kyc")]
        public async Task<IActionResult> SubmitKyc([FromBody] SubmitKycDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { error = "Address, Government ID, and ID Type are required" });

            var user = await _db.Users.FindAsync(GetUserId());
            if (user == null) return NotFound(new { error = "User profile not found" });

            user.Address = dto.Address;
            user.GovtId = dto.GovtId;
            user.GovtIdType = dto.GovtIdType;

            // Upgrade user → owner if needed
            if (user.Role == "user") user.Role = "owner";

            // Must wait for admin to approve
            user.Verified = false;
            user.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "KYC documents submitted successfully. Please wait for administrator verification.",
                user = new { id = user.Id, role = user.Role, verified = user.Verified }
            });
        }
    }
}
