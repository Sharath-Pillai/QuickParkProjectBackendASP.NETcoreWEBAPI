using System.ComponentModel.DataAnnotations;

namespace QuickParkAPI.Models
{
    public class Review
    {
        public int Id { get; set; }

        public int ParkingSlotId { get; set; }
        public ParkingSlot ParkingSlot { get; set; } = null!;

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string UserName { get; set; } = string.Empty;

        public string Comment { get; set; } = string.Empty;

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
