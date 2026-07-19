using System.ComponentModel.DataAnnotations;

namespace QuickParkAPI.DTOs
{
    // ── Auth DTOs ─────────────────────────────────────────────────────────────
    public class RegisterDto
    {
        [Required] public string Name { get; set; } = string.Empty;
        [Required] public string Email { get; set; } = string.Empty;
        [Required] [MinLength(6)] public string Password { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Role { get; set; } // "user" | "owner"
        public string? Address { get; set; }
        public string? GovtId { get; set; }
        public string? GovtIdType { get; set; }
    }

    public class LoginDto
    {
        [Required] public string Email { get; set; } = string.Empty;
        [Required] public string Password { get; set; } = string.Empty;
    }

    public class UserResponseDto
    {
        public int Id { get; set; }
        // _id alias for MongoDB-style frontend compatibility
        [System.Text.Json.Serialization.JsonPropertyName("_id")]
        public string MongoId => Id.ToString();
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool Verified { get; set; }
        public bool IsActive { get; set; }
        public string Phone { get; set; } = string.Empty;
        public string ProfileImage { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string GovtId { get; set; } = string.Empty;
        public string GovtIdType { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class AuthResponseDto
    {
        public bool Success { get; set; }
        public string Token { get; set; } = string.Empty;
        public UserResponseDto? User { get; set; }
        public bool PendingApproval { get; set; }
    }

    // ── Profile DTOs ──────────────────────────────────────────────────────────
    public class UpdateProfileDto
    {
        public string? Name { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? ProfileImage { get; set; }
    }

    public class SubmitKycDto
    {
        [Required] public string Address { get; set; } = string.Empty;
        [Required] public string GovtId { get; set; } = string.Empty;
        [Required] public string GovtIdType { get; set; } = string.Empty;
    }

    // ── ParkingSlot DTOs ──────────────────────────────────────────────────────
    public class LocationDto
    {
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Pincode { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class CreateParkingSlotDto
    {
        [Required] public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        [Required] public LocationDto Location { get; set; } = new();
        [Required] public int TotalSlots { get; set; }
        [Required] public List<string> VehicleTypes { get; set; } = new();
        [Required] public decimal PricePerHour { get; set; }
        public decimal? PricePerDay { get; set; }
        public decimal? PricePerMonth { get; set; }
        public List<string>? Amenities { get; set; }
        public List<string>? Photos { get; set; }
        public string? OpeningTime { get; set; }
        public string? ClosingTime { get; set; }
        public bool AutoApprove { get; set; } = false;
    }

    public class UpdateParkingSlotDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public LocationDto? Location { get; set; }
        public int? TotalSlots { get; set; }
        public List<string>? VehicleTypes { get; set; }
        public decimal? PricePerHour { get; set; }
        public decimal? PricePerDay { get; set; }
        public decimal? PricePerMonth { get; set; }
        public List<string>? Amenities { get; set; }
        public List<string>? Photos { get; set; }
        public string? OpeningTime { get; set; }
        public string? ClosingTime { get; set; }
    }

    public class OwnerInfoDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ProfileImage { get; set; } = string.Empty;
    }

    public class ReviewResponseDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public int Rating { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ParkingSlotResponseDto
    {
        public int Id { get; set; }
        // _id alias for MongoDB-style frontend compatibility
        public string _id => Id.ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public OwnerInfoDto? Owner { get; set; }
        public LocationDto Location { get; set; } = new();
        public int TotalSlots { get; set; }
        public int AvailableSlots { get; set; }
        public List<string> VehicleTypes { get; set; } = new();
        public decimal PricePerHour { get; set; }
        public decimal? PricePerDay { get; set; }
        public decimal? PricePerMonth { get; set; }
        public List<string> Amenities { get; set; } = new();
        public List<string> Photos { get; set; } = new();
        public double Rating { get; set; }
        public int TotalReviews { get; set; }
        public List<ReviewResponseDto> Reviews { get; set; } = new();
        public string Status { get; set; } = string.Empty;
        public bool AutoApprove { get; set; }
        public bool IsActive { get; set; }
        public string OpeningTime { get; set; } = string.Empty;
        public string ClosingTime { get; set; } = string.Empty;
        public List<DateTime> BlockedDates { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class AddReviewDto
    {
        [Required] [Range(1, 5)] public int Rating { get; set; }
        public string? Comment { get; set; }
    }

    public class BlockDatesDto
    {
        [Required] public List<DateTime> Dates { get; set; } = new();
    }

    // ── Booking DTOs ──────────────────────────────────────────────────────────
    public class CreateBookingDto
    {
        [Required] public int ParkingSlotId { get; set; }
        public string? SlotNumber { get; set; }
        [Required] public string VehicleRegNumber { get; set; } = string.Empty;
        [Required] public string VehicleType { get; set; } = string.Empty;
        [Required] public DateTime StartDate { get; set; }
        [Required] public DateTime EndDate { get; set; }
        [Required] public double Hours { get; set; }
    }

    public class UpdateStatusDto
    {
        [Required] public string Status { get; set; } = string.Empty;
    }

    public class ConfirmPaymentDto
    {
        public string? PaymentMethod { get; set; }
        public string? PaymentId { get; set; }
        public string? GatewayOrderId { get; set; }
    }

    public class InitiatePaymentDto
    {
        [Required] public string Gateway { get; set; } = string.Empty;
    }

    public class ParkingSlotSummaryDto
    {
        public int Id { get; set; }
        public string _id => Id.ToString();
        public string Name { get; set; } = string.Empty;
        public LocationDto Location { get; set; } = new();
        public decimal PricePerHour { get; set; }
        public List<string> Photos { get; set; } = new();
    }

    public class UserSummaryDto
    {
        public int Id { get; set; }
        public string _id => Id.ToString();
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
    }

    public class BookingResponseDto
    {
        public int Id { get; set; }
        // _id alias for MongoDB-style frontend compatibility
        public string _id => Id.ToString();
        public UserSummaryDto? User { get; set; }
        public ParkingSlotSummaryDto? ParkingSlot { get; set; }
        public string SlotNumber { get; set; } = string.Empty;
        public string VehicleRegNumber { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public double Hours { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentId { get; set; } = string.Empty;
        public string PaymentGatewayOrderId { get; set; } = string.Empty;
        public string QrCode { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    // ── Admin DTOs ────────────────────────────────────────────────────────────
    public class DashboardStatsDto
    {
        public int TotalUsers { get; set; }
        public int TotalOwners { get; set; }
        public int VerifiedOwners { get; set; }
        public int PendingOwners { get; set; }
        public int TotalListings { get; set; }
        public int PendingListings { get; set; }
        public int TotalBookings { get; set; }
        public int ConfirmedBookings { get; set; }
        public int PendingBookings { get; set; }
        public int CancelledBookings { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
