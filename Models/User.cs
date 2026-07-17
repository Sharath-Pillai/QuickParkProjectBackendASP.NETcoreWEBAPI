using System.ComponentModel.DataAnnotations;

namespace QuickParkAPI.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        // "user" | "owner" | "admin"
        [MaxLength(20)]
        public string Role { get; set; } = "user";

        public bool Verified { get; set; } = false;
        public bool IsActive { get; set; } = true;

        public string ProfileImage { get; set; } = string.Empty;

        // Owner KYC fields
        public string Address { get; set; } = string.Empty;
        public string GovtId { get; set; } = string.Empty;

        // "aadhaar" | "pan" | "passport" | "driving_license" | ""
        [MaxLength(50)]
        public string GovtIdType { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<ParkingSlot> ParkingSlots { get; set; } = new List<ParkingSlot>();
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
