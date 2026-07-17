namespace QuickParkAPI.Models
{
    public class BlockedDate
    {
        public int Id { get; set; }
        public int ParkingSlotId { get; set; }
        public ParkingSlot ParkingSlot { get; set; } = null!;
        public DateTime Date { get; set; }
    }
}
