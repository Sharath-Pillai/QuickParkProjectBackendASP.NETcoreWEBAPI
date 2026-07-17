using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuickParkAPI.Models
{
    public class ParkingSlot
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        // FK to User (owner)
        public int OwnerId { get; set; }
        public User Owner { get; set; } = null!;

        // Location
        [Required]
        public string Address { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string City { get; set; } = string.Empty;

        public string State { get; set; } = string.Empty;
        public string Pincode { get; set; } = string.Empty;

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        // Capacity
        [Required]
        public int TotalSlots { get; set; }
        public int AvailableSlots { get; set; }

        // Vehicle types stored as JSON array string e.g. ["2-wheeler","4-wheeler"]
        public string VehicleTypesJson { get; set; } = "[]";

        // Pricing
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal PricePerHour { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? PricePerDay { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? PricePerMonth { get; set; }

        // Arrays stored as JSON strings
        public string AmenitiesJson { get; set; } = "[]";
        public string PhotosJson { get; set; } = "[]";

        // Ratings
        public double Rating { get; set; } = 0;
        public int TotalReviews { get; set; } = 0;

        // Status: "pending" | "approved" | "rejected"
        [MaxLength(20)]
        public string Status { get; set; } = "pending";

        public bool AutoApprove { get; set; } = false;
        public bool IsActive { get; set; } = true;

        [MaxLength(10)]
        public string OpeningTime { get; set; } = "00:00";

        [MaxLength(10)]
        public string ClosingTime { get; set; } = "23:59";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<BlockedDate> BlockedDates { get; set; } = new List<BlockedDate>();
    }
}
