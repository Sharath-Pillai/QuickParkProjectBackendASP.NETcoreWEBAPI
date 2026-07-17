using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickParkAPI.Data;
using QuickParkAPI.DTOs;
using QuickParkAPI.Models;
using QuickParkAPI.Services;

namespace QuickParkAPI.Controllers
{
    [ApiController]
    [Route("api/bookings")]
    [Authorize]
    public class BookingsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public BookingsController(AppDbContext db) => _db = db;

        private int GetUserId() => ClaimsHelper.GetUserId(User);
        private string GetUserRole() => ClaimsHelper.GetRole(User);

        // ── SPECIFIC ROUTES FIRST ─────────────────────────────────────────────

        // GET /api/bookings/user/bookings
        [HttpGet("user/bookings")]
        public async Task<IActionResult> GetUserBookings()
        {
            if (GetUserRole() != "user")
                return StatusCode(403, new { error = "Access denied. This action requires role: user." });

            var bookings = await _db.Bookings
                .Include(b => b.User)
                .Include(b => b.ParkingSlot)
                .Where(b => b.UserId == GetUserId())
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return Ok(bookings.Select(MappingHelper.ToBookingResponse));
        }

        // GET /api/bookings/owner/all
        [HttpGet("owner/all")]
        public async Task<IActionResult> GetOwnerBookings()
        {
            if (GetUserRole() != "owner")
                return StatusCode(403, new { error = "Access denied. This action requires role: owner." });

            var slotIds = await _db.ParkingSlots
                .Where(s => s.OwnerId == GetUserId())
                .Select(s => s.Id)
                .ToListAsync();

            var bookings = await _db.Bookings
                .Include(b => b.User)
                .Include(b => b.ParkingSlot)
                .Where(b => slotIds.Contains(b.ParkingSlotId))
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return Ok(bookings.Select(MappingHelper.ToBookingResponse));
        }

        // GET /api/bookings/admin/all
        [HttpGet("admin/all")]
        public async Task<IActionResult> GetAdminBookings()
        {
            if (GetUserRole() != "admin")
                return StatusCode(403, new { error = "Access denied. This action requires role: admin." });

            var bookings = await _db.Bookings
                .Include(b => b.User)
                .Include(b => b.ParkingSlot)
                    .ThenInclude(p => p!.Owner)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return Ok(bookings.Select(MappingHelper.ToBookingResponse));
        }

        // POST /api/bookings
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateBookingDto dto)
        {
            if (GetUserRole() != "user")
                return StatusCode(403, new { error = "Access denied. This action requires role: user." });

            if (dto.ParkingSlotId == 0 || string.IsNullOrEmpty(dto.VehicleRegNumber) ||
                string.IsNullOrEmpty(dto.VehicleType) || dto.Hours <= 0)
                return BadRequest(new { error = "Missing required booking details" });

            var slot = await _db.ParkingSlots.FindAsync(dto.ParkingSlotId);
            if (slot == null) return NotFound(new { error = "Parking slot not found" });

            if (slot.Status != "approved" || !slot.IsActive)
                return BadRequest(new { error = "This parking space is currently unavailable for booking" });

            if (slot.AvailableSlots <= 0)
                return BadRequest(new { error = "No available spots remaining in this parking slot" });

            var totalPrice = Math.Round((double)slot.PricePerHour * dto.Hours, 2);
            var rnd = new Random();
            var slotNumber = dto.SlotNumber ?? $"P-{rnd.Next(100, 999)}";

            var booking = new Booking
            {
                UserId = GetUserId(),
                ParkingSlotId = dto.ParkingSlotId,
                SlotNumber = slotNumber,
                VehicleRegNumber = dto.VehicleRegNumber.ToUpper().Trim(),
                VehicleType = dto.VehicleType,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Hours = dto.Hours,
                TotalPrice = (decimal)totalPrice,
                Status = "pending"
            };

            _db.Bookings.Add(booking);
            await _db.SaveChangesAsync();

            // Reload with nav props
            booking = await _db.Bookings
                .Include(b => b.User)
                .Include(b => b.ParkingSlot)
                .FirstAsync(b => b.Id == booking.Id);

            return StatusCode(201, MappingHelper.ToBookingResponse(booking));
        }

        // GET /api/bookings/:id
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var booking = await _db.Bookings
                .Include(b => b.User)
                .Include(b => b.ParkingSlot)
                    .ThenInclude(p => p!.Owner)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null) return NotFound(new { error = "Booking not found" });

            var userId = GetUserId();
            var role = GetUserRole();
            var isUser = booking.UserId == userId;
            var isOwner = booking.ParkingSlot?.OwnerId == userId;
            var isAdmin = role == "admin";

            if (!isUser && !isOwner && !isAdmin)
                return StatusCode(403, new { error = "Not authorized to view this booking" });

            return Ok(MappingHelper.ToBookingResponse(booking));
        }

        // PUT /api/bookings/:id/status
        [HttpPut("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto dto)
        {
            var booking = await _db.Bookings.FindAsync(id);
            if (booking == null) return NotFound(new { error = "Booking not found" });

            var slot = await _db.ParkingSlots.FindAsync(booking.ParkingSlotId);
            var isOwner = slot?.OwnerId == GetUserId();
            var isAdmin = GetUserRole() == "admin";

            if (!isOwner && !isAdmin)
                return StatusCode(403, new { error = "Not authorized to update status" });

            var oldStatus = booking.Status;
            booking.Status = dto.Status;
            booking.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            if (dto.Status == "cancelled" && oldStatus == "confirmed" && slot != null)
            {
                slot.AvailableSlots++;
                slot.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            // Reload
            booking = await _db.Bookings.Include(b => b.User).Include(b => b.ParkingSlot).FirstAsync(b => b.Id == id);
            return Ok(MappingHelper.ToBookingResponse(booking));
        }

        // POST /api/bookings/:id/initiate-payment
        [HttpPost("{id:int}/initiate-payment")]
        public async Task<IActionResult> InitiatePayment(int id, [FromBody] InitiatePaymentDto dto)
        {
            if (GetUserRole() != "user")
                return StatusCode(403, new { error = "Access denied. This action requires role: user." });

            var booking = await _db.Bookings.FindAsync(id);
            if (booking == null) return NotFound(new { error = "Booking not found" });
            if (booking.UserId != GetUserId()) return StatusCode(403, new { error = "Not authorized" });
            if (booking.Status == "confirmed") return BadRequest(new { error = "Booking is already confirmed" });

            var amount = booking.TotalPrice;
            var currency = "INR";
            var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var shortId = booking.Id.ToString().PadLeft(6, '0').ToUpper();
            var rnd = new Random();

            object gatewayData;

            if (dto.Gateway == "stripe")
            {
                var pid = $"pi_3{shortId}{ts}";
                gatewayData = new
                {
                    gateway = "stripe",
                    paymentIntentId = pid,
                    clientSecret = $"{pid}_secret_dummy{rnd.Next(100000, 999999)}",
                    amount = (long)(amount * 100),
                    currency,
                    status = "requires_payment_method",
                    publishableKey = "pk_test_DUMMY_KEY_quickpark"
                };
                booking.PaymentGatewayOrderId = pid;
            }
            else if (dto.Gateway == "razorpay")
            {
                var oid = $"order_{shortId}{ts}";
                gatewayData = new
                {
                    gateway = "razorpay",
                    orderId = oid,
                    amount = (long)(amount * 100),
                    currency,
                    status = "created",
                    keyId = "rzp_test_DUMMY_KEY_quickpark",
                    receipt = $"receipt_{booking.Id}"
                };
                booking.PaymentGatewayOrderId = oid;
            }
            else
            {
                return BadRequest(new { error = "Unsupported gateway. Use stripe or razorpay." });
            }

            booking.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new { success = true, gateway = dto.Gateway, data = gatewayData });
        }

        // PUT /api/bookings/:id/confirm-payment
        [HttpPut("{id:int}/confirm-payment")]
        public async Task<IActionResult> ConfirmPayment(int id, [FromBody] ConfirmPaymentDto dto)
        {
            var booking = await _db.Bookings.FindAsync(id);
            if (booking == null) return NotFound(new { error = "Booking not found" });
            if (booking.UserId != GetUserId()) return StatusCode(403, new { error = "Not authorized to make payment" });
            if (booking.Status == "confirmed") return BadRequest(new { error = "Booking is already confirmed" });

            var slot = await _db.ParkingSlots.FindAsync(booking.ParkingSlotId);
            if (slot == null || slot.AvailableSlots <= 0)
                return BadRequest(new { error = "Parking slot is fully booked now" });

            var isCOD = dto.PaymentMethod == "cod";
            var rnd = new Random();
            var shortId = booking.Id.ToString().PadLeft(6, '0').ToUpper();
            var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            booking.PaymentStatus = isCOD ? "pending" : "completed";
            booking.PaymentMethod = dto.PaymentMethod ?? "upi";
            booking.PaymentId = isCOD
                ? $"COD_{shortId}_{ts}"
                : (dto.PaymentId ?? $"PAY_{rnd.Next(100000, 999999)}");
            if (!string.IsNullOrEmpty(dto.GatewayOrderId)) booking.PaymentGatewayOrderId = dto.GatewayOrderId;
            booking.Status = "confirmed";
            booking.QrCode = $"QR_{booking.Id}_{ts}";
            booking.UpdatedAt = DateTime.UtcNow;

            slot.AvailableSlots = Math.Max(0, slot.AvailableSlots - 1);
            slot.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            // Reload
            booking = await _db.Bookings.Include(b => b.User).Include(b => b.ParkingSlot).FirstAsync(b => b.Id == id);
            return Ok(MappingHelper.ToBookingResponse(booking));
        }

        // PUT /api/bookings/:id/cancel
        [HttpPut("{id:int}/cancel")]
        public async Task<IActionResult> Cancel(int id)
        {
            var booking = await _db.Bookings.FindAsync(id);
            if (booking == null) return NotFound(new { error = "Booking not found" });

            var slot = await _db.ParkingSlots.FindAsync(booking.ParkingSlotId);
            var userId = GetUserId();
            var role = GetUserRole();
            var isUser = booking.UserId == userId;
            var isOwner = slot?.OwnerId == userId;
            var isAdmin = role == "admin";

            if (!isUser && !isOwner && !isAdmin)
                return StatusCode(403, new { error = "Not authorized to cancel this booking" });

            if (booking.Status == "cancelled")
                return BadRequest(new { error = "Booking already cancelled" });

            var wasConfirmed = booking.Status == "confirmed";
            booking.Status = "cancelled";
            if (booking.PaymentStatus == "completed") booking.PaymentStatus = "refunded";
            booking.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            if (wasConfirmed && slot != null)
            {
                slot.AvailableSlots = Math.Min(slot.TotalSlots, slot.AvailableSlots + 1);
                slot.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            // Reload
            booking = await _db.Bookings.Include(b => b.User).Include(b => b.ParkingSlot).FirstAsync(b => b.Id == id);
            return Ok(MappingHelper.ToBookingResponse(booking));
        }
    }
}
