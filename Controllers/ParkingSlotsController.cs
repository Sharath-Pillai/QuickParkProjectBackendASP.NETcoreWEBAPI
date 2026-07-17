using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickParkAPI.Data;
using QuickParkAPI.DTOs;
using QuickParkAPI.Models;
using QuickParkAPI.Services;
using System.Text.Json;

namespace QuickParkAPI.Controllers
{
    [ApiController]
    [Route("api/parking-slots")]
    public class ParkingSlotsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ParkingSlotsController(AppDbContext db) => _db = db;

        private int GetUserId() => ClaimsHelper.GetUserId(User);
        private string GetUserRole() => ClaimsHelper.GetRole(User);

        // ── SPECIFIC ROUTES FIRST (before /:id) ──────────────────────────────

        // GET /api/parking-slots/owner/slots
        [HttpGet("owner/slots")]
        [Authorize]
        public async Task<IActionResult> GetOwnerSlots()
        {
            if (GetUserRole() != "owner")
                return StatusCode(403, new { error = "Access denied. This action requires role: owner." });

            var slots = await _db.ParkingSlots
                .Include(s => s.Owner)
                .Include(s => s.Reviews)
                .Include(s => s.BlockedDates)
                .Where(s => s.OwnerId == GetUserId())
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return Ok(slots.Select(MappingHelper.ToSlotResponse));
        }

        // GET /api/parking-slots/admin/pending
        [HttpGet("admin/pending")]
        [Authorize]
        public async Task<IActionResult> GetPendingSlots()
        {
            if (GetUserRole() != "admin")
                return StatusCode(403, new { error = "Access denied. This action requires role: admin." });

            var slots = await _db.ParkingSlots
                .Include(s => s.Owner)
                .Include(s => s.Reviews)
                .Include(s => s.BlockedDates)
                .Where(s => s.Status == "pending")
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return Ok(slots.Select(MappingHelper.ToSlotResponse));
        }

        // ── PUBLIC ROUTES ─────────────────────────────────────────────────────

        // GET /api/parking-slots?city=&vehicleType=&search=&minPrice=&maxPrice=
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? city,
            [FromQuery] string? vehicleType,
            [FromQuery] string? search,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice)
        {
            var query = _db.ParkingSlots
                .Include(s => s.Owner)
                .Include(s => s.Reviews)
                .Include(s => s.BlockedDates)
                .Where(s => s.Status == "approved" && s.IsActive);

            if (!string.IsNullOrEmpty(city))
                query = query.Where(s => s.City.ToLower().Contains(city.ToLower()));

            if (!string.IsNullOrEmpty(vehicleType))
                query = query.Where(s => s.VehicleTypesJson.Contains(vehicleType));

            if (!string.IsNullOrEmpty(search))
                query = query.Where(s =>
                    s.Name.ToLower().Contains(search.ToLower()) ||
                    s.Address.ToLower().Contains(search.ToLower()) ||
                    s.City.ToLower().Contains(search.ToLower()));

            if (minPrice.HasValue)
                query = query.Where(s => s.PricePerHour >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(s => s.PricePerHour <= maxPrice.Value);

            var slots = await query
                .OrderByDescending(s => s.Rating)
                .ThenByDescending(s => s.CreatedAt)
                .ToListAsync();

            return Ok(slots.Select(MappingHelper.ToSlotResponse));
        }

        // GET /api/parking-slots/:id
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var slot = await _db.ParkingSlots
                .Include(s => s.Owner)
                .Include(s => s.Reviews)
                .Include(s => s.BlockedDates)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (slot == null) return NotFound(new { error = "Parking slot not found" });
            return Ok(MappingHelper.ToSlotResponse(slot));
        }

        // POST /api/parking-slots
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CreateParkingSlotDto dto)
        {
            if (GetUserRole() != "owner")
                return StatusCode(403, new { error = "Access denied. This action requires role: owner." });

            if (string.IsNullOrEmpty(dto.Name) || dto.Location == null ||
                string.IsNullOrEmpty(dto.Location.Address) || string.IsNullOrEmpty(dto.Location.City) ||
                dto.TotalSlots <= 0 || dto.VehicleTypes == null || dto.PricePerHour <= 0)
            {
                return BadRequest(new { error = "Missing required fields for listing a parking slot" });
            }

            var slot = new ParkingSlot
            {
                Name = dto.Name,
                Description = dto.Description ?? string.Empty,
                OwnerId = GetUserId(),
                Address = dto.Location.Address,
                City = dto.Location.City,
                State = dto.Location.State ?? string.Empty,
                Pincode = dto.Location.Pincode ?? string.Empty,
                Latitude = dto.Location.Latitude,
                Longitude = dto.Location.Longitude,
                TotalSlots = dto.TotalSlots,
                AvailableSlots = dto.TotalSlots,
                VehicleTypesJson = MappingHelper.ToJsonArray(dto.VehicleTypes),
                PricePerHour = dto.PricePerHour,
                PricePerDay = dto.PricePerDay,
                PricePerMonth = dto.PricePerMonth,
                AmenitiesJson = MappingHelper.ToJsonArray(dto.Amenities),
                PhotosJson = MappingHelper.ToJsonArray(dto.Photos),
                OpeningTime = dto.OpeningTime ?? "00:00",
                ClosingTime = dto.ClosingTime ?? "23:59",
                AutoApprove = dto.AutoApprove,
                Status = dto.AutoApprove ? "approved" : "pending"
            };

            _db.ParkingSlots.Add(slot);
            await _db.SaveChangesAsync();

            // Reload with owner
            slot = await _db.ParkingSlots
                .Include(s => s.Owner)
                .Include(s => s.Reviews)
                .Include(s => s.BlockedDates)
                .FirstAsync(s => s.Id == slot.Id);

            return StatusCode(201, MappingHelper.ToSlotResponse(slot));
        }

        // PUT /api/parking-slots/:id
        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateParkingSlotDto dto)
        {
            if (GetUserRole() != "owner")
                return StatusCode(403, new { error = "Access denied. This action requires role: owner." });

            var slot = await _db.ParkingSlots
                .Include(s => s.Owner)
                .Include(s => s.Reviews)
                .Include(s => s.BlockedDates)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (slot == null) return NotFound(new { error = "Parking slot not found" });

            if (slot.OwnerId != GetUserId())
                return StatusCode(403, new { error = "Not authorized to update this listing" });

            if (dto.Name != null) slot.Name = dto.Name;
            if (dto.Description != null) slot.Description = dto.Description;
            if (dto.Location != null)
            {
                slot.Address = dto.Location.Address;
                slot.City = dto.Location.City;
                slot.State = dto.Location.State ?? slot.State;
                slot.Pincode = dto.Location.Pincode ?? slot.Pincode;
                slot.Latitude = dto.Location.Latitude;
                slot.Longitude = dto.Location.Longitude;
            }
            if (dto.TotalSlots.HasValue) slot.TotalSlots = dto.TotalSlots.Value;
            if (dto.VehicleTypes != null) slot.VehicleTypesJson = MappingHelper.ToJsonArray(dto.VehicleTypes);
            if (dto.PricePerHour.HasValue) slot.PricePerHour = dto.PricePerHour.Value;
            if (dto.PricePerDay.HasValue) slot.PricePerDay = dto.PricePerDay.Value;
            if (dto.PricePerMonth.HasValue) slot.PricePerMonth = dto.PricePerMonth.Value;
            if (dto.Amenities != null) slot.AmenitiesJson = MappingHelper.ToJsonArray(dto.Amenities);
            if (dto.Photos != null) slot.PhotosJson = MappingHelper.ToJsonArray(dto.Photos);
            if (dto.OpeningTime != null) slot.OpeningTime = dto.OpeningTime;
            if (dto.ClosingTime != null) slot.ClosingTime = dto.ClosingTime;
            slot.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Ok(MappingHelper.ToSlotResponse(slot));
        }

        // POST /api/parking-slots/:id/reviews
        [HttpPost("{id:int}/reviews")]
        [Authorize]
        public async Task<IActionResult> AddReview(int id, [FromBody] AddReviewDto dto)
        {
            if (dto.Rating < 1 || dto.Rating > 5)
                return BadRequest(new { error = "Rating must be between 1 and 5" });

            var slot = await _db.ParkingSlots
                .Include(s => s.Reviews)
                .Include(s => s.Owner)
                .Include(s => s.BlockedDates)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (slot == null) return NotFound(new { error = "Parking slot not found" });

            if (slot.OwnerId == GetUserId())
                return BadRequest(new { error = "Owners cannot review their own parking spaces" });

            var userName = ClaimsHelper.GetName(User);

            var review = new Review
            {
                ParkingSlotId = id,
                UserId = GetUserId(),
                UserName = userName,
                Comment = dto.Comment ?? string.Empty,
                Rating = dto.Rating
            };

            _db.Reviews.Add(review);
            await _db.SaveChangesAsync();

            // Reload reviews and recalculate
            slot.Reviews = await _db.Reviews.Where(r => r.ParkingSlotId == id).ToListAsync();
            slot.TotalReviews = slot.Reviews.Count;
            slot.Rating = Math.Round(slot.Reviews.Average(r => r.Rating), 1);
            await _db.SaveChangesAsync();

            return StatusCode(201, new { success = true, slot = MappingHelper.ToSlotResponse(slot) });
        }

        // POST /api/parking-slots/:id/block-dates
        [HttpPost("{id:int}/block-dates")]
        [Authorize]
        public async Task<IActionResult> BlockDates(int id, [FromBody] BlockDatesDto dto)
        {
            if (GetUserRole() != "owner")
                return StatusCode(403, new { error = "Access denied. This action requires role: owner." });

            if (dto.Dates == null || !dto.Dates.Any())
                return BadRequest(new { error = "Dates array must be provided" });

            var slot = await _db.ParkingSlots
                .Include(s => s.Owner)
                .Include(s => s.Reviews)
                .Include(s => s.BlockedDates)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (slot == null) return NotFound(new { error = "Parking slot not found" });
            if (slot.OwnerId != GetUserId()) return StatusCode(403, new { error = "Not authorized" });

            foreach (var d in dto.Dates)
                _db.BlockedDates.Add(new BlockedDate { ParkingSlotId = id, Date = d });

            await _db.SaveChangesAsync();

            // Reload
            slot.BlockedDates = await _db.BlockedDates.Where(b => b.ParkingSlotId == id).ToListAsync();
            return Ok(MappingHelper.ToSlotResponse(slot));
        }

        // PUT /api/parking-slots/:id/approve
        [HttpPut("{id:int}/approve")]
        [Authorize]
        public async Task<IActionResult> Approve(int id)
        {
            if (GetUserRole() != "admin")
                return StatusCode(403, new { error = "Access denied. This action requires role: admin." });

            var slot = await _db.ParkingSlots
                .Include(s => s.Owner)
                .Include(s => s.Reviews)
                .Include(s => s.BlockedDates)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (slot == null) return NotFound(new { error = "Parking slot not found" });

            slot.Status = "approved";
            slot.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(MappingHelper.ToSlotResponse(slot));
        }

        // PUT /api/parking-slots/:id/reject
        [HttpPut("{id:int}/reject")]
        [Authorize]
        public async Task<IActionResult> Reject(int id)
        {
            if (GetUserRole() != "admin")
                return StatusCode(403, new { error = "Access denied. This action requires role: admin." });

            var slot = await _db.ParkingSlots
                .Include(s => s.Owner)
                .Include(s => s.Reviews)
                .Include(s => s.BlockedDates)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (slot == null) return NotFound(new { error = "Parking slot not found" });

            slot.Status = "rejected";
            slot.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(MappingHelper.ToSlotResponse(slot));
        }
    }
}
