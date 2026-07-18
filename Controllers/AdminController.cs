using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickParkAPI.Data;
using QuickParkAPI.DTOs;
using QuickParkAPI.Services;

namespace QuickParkAPI.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _db;

        public AdminController(AppDbContext db) => _db = db;

        private string GetUserRole() => ClaimsHelper.GetRole(User);

        private IActionResult AdminOnly()
        {
            if (GetUserRole() != "admin")
                return StatusCode(403, new { error = "Access denied. This action requires role: admin." });
            return null!;
        }

        // GET /api/admin/users
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var guard = AdminOnly(); if (guard != null) return guard;

            var users = await _db.Users
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            return Ok(users.Select(MappingHelper.ToUserResponse));
        }

        // PUT /api/admin/users/:id/toggle-status
        [HttpPut("users/{id:int}/toggle-status")]
        public async Task<IActionResult> ToggleUserStatus(int id)
        {
            var guard = AdminOnly(); if (guard != null) return guard;

            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound(new { error = "User not found" });
            if (user.Role == "admin") return BadRequest(new { error = "Cannot deactivate an administrator account" });

            user.IsActive = !user.IsActive;
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = $"User account has been {(user.IsActive ? "activated" : "deactivated")} successfully",
                user = new { id = user.Id, name = user.Name, email = user.Email, isActive = user.IsActive, role = user.Role }
            });
        }

        // DELETE /api/admin/users/:id
        [HttpDelete("users/{id:int}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var guard = AdminOnly(); if (guard != null) return guard;

            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound(new { error = "User not found" });
            if (user.Role == "admin") return BadRequest(new { error = "Cannot delete an administrator account" });

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();

            return Ok(new { success = true, message = "User account deleted successfully" });
        }

        // GET /api/admin/pending-owners
        [HttpGet("pending-owners")]
        public async Task<IActionResult> GetPendingOwners()
        {
            var guard = AdminOnly(); if (guard != null) return guard;

            var owners = await _db.Users
                .Where(u => u.Role == "owner" && !u.Verified)
                .ToListAsync();

            return Ok(owners.Select(MappingHelper.ToUserResponse));
        }

        // PUT /api/admin/verify-owner/:id
        [HttpPut("verify-owner/{id:int}")]
        public async Task<IActionResult> VerifyOwner(int id)
        {
            var guard = AdminOnly(); if (guard != null) return guard;

            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound(new { error = "Owner not found" });

            user.Verified = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(MappingHelper.ToUserResponse(user));
        }

        // PUT /api/admin/reject-owner/:id
        [HttpPut("reject-owner/{id:int}")]
        public async Task<IActionResult> RejectOwner(int id)
        {
            var guard = AdminOnly(); if (guard != null) return guard;

            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound(new { error = "User not found" });

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();

            return Ok(new { success = true, message = "Owner registration request rejected and account removed" });
        }

        // GET /api/admin/pending-listings
        [HttpGet("pending-listings")]
        public async Task<IActionResult> GetPendingListings()
        {
            var guard = AdminOnly(); if (guard != null) return guard;

            var listings = await _db.ParkingSlots
                .Include(s => s.Owner)
                .Include(s => s.Reviews)
                .Include(s => s.BlockedDates)
                .Where(s => s.Status == "pending")
                .ToListAsync();

            return Ok(listings.Select(MappingHelper.ToSlotResponse));
        }

        // PUT /api/admin/verify-listing/:id
        [HttpPut("verify-listing/{id:int}")]
        public async Task<IActionResult> VerifyListing(int id)
        {
            var guard = AdminOnly(); if (guard != null) return guard;

            var slot = await _db.ParkingSlots
                .Include(s => s.Owner)
                .Include(s => s.Reviews)
                .Include(s => s.BlockedDates)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (slot == null) return NotFound(new { error = "Parking listing not found" });

            slot.Status = "approved";
            slot.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(MappingHelper.ToSlotResponse(slot));
        }

        // PUT /api/admin/reject-listing/:id
        [HttpPut("reject-listing/{id:int}")]
        public async Task<IActionResult> RejectListing(int id)
        {
            var guard = AdminOnly(); if (guard != null) return guard;

            var slot = await _db.ParkingSlots
                .Include(s => s.Owner)
                .Include(s => s.Reviews)
                .Include(s => s.BlockedDates)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (slot == null) return NotFound(new { error = "Parking listing not found" });

            slot.Status = "rejected";
            slot.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(MappingHelper.ToSlotResponse(slot));
        }

        // GET /api/admin/dashboard/stats
        [HttpGet("dashboard/stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var guard = AdminOnly(); if (guard != null) return guard;

            var totalUsers = await _db.Users.CountAsync(u => u.Role == "user");
            var totalOwners = await _db.Users.CountAsync(u => u.Role == "owner");
            var verifiedOwners = await _db.Users.CountAsync(u => u.Role == "owner" && u.Verified);
            var pendingOwners = await _db.Users.CountAsync(u => u.Role == "owner" && !u.Verified);
            var totalListings = await _db.ParkingSlots.CountAsync(s => s.Status == "approved");
            var pendingListings = await _db.ParkingSlots.CountAsync(s => s.Status == "pending");
            var totalBookings = await _db.Bookings.CountAsync();
            var confirmedBookings = await _db.Bookings.CountAsync(b => b.Status == "confirmed");
            var pendingBookings = await _db.Bookings.CountAsync(b => b.Status == "pending");
            var cancelledBookings = await _db.Bookings.CountAsync(b => b.Status == "cancelled");
            var totalRevenue = await _db.Bookings
                .Where(b => b.PaymentStatus == "completed")
                .SumAsync(b => (decimal?)b.TotalPrice) ?? 0;

            return Ok(new DashboardStatsDto
            {
                TotalUsers = totalUsers,
                TotalOwners = totalOwners,
                VerifiedOwners = verifiedOwners,
                PendingOwners = pendingOwners,
                TotalListings = totalListings,
                PendingListings = pendingListings,
                TotalBookings = totalBookings,
                ConfirmedBookings = confirmedBookings,
                PendingBookings = pendingBookings,
                CancelledBookings = cancelledBookings,
                TotalRevenue = totalRevenue
            });
        }
    }
}
