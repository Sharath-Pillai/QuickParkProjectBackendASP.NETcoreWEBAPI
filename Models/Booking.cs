using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuickParkAPI.Models
{
    public class Booking
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public int ParkingSlotId { get; set; }
        public ParkingSlot ParkingSlot { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string SlotNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string VehicleRegNumber { get; set; } = string.Empty;

        // "2-wheeler" | "4-wheeler"
        [Required]
        [MaxLength(20)]
        public string VehicleType { get; set; } = string.Empty;

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public double Hours { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        // "pending" | "confirmed" | "active" | "completed" | "cancelled"
        [MaxLength(20)]
        public string Status { get; set; } = "pending";

        // "pending" | "completed" | "failed" | "refunded"
        [MaxLength(20)]
        public string PaymentStatus { get; set; } = "pending";

        // "upi" | "card" | "wallet" | "cod" | "stripe" | "razorpay" | ""
        [MaxLength(20)]
        public string PaymentMethod { get; set; } = string.Empty;

        public string PaymentId { get; set; } = string.Empty;
        public string PaymentGatewayOrderId { get; set; } = string.Empty;
        public string QrCode { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
