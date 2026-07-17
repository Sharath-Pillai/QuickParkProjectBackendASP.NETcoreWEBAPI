using System.Text.Json;
using QuickParkAPI.DTOs;
using QuickParkAPI.Models;

namespace QuickParkAPI.Services
{
    public static class MappingHelper
    {
        public static UserResponseDto ToUserResponse(User u) => new()
        {
            Id = u.Id,
            Name = u.Name,
            Email = u.Email,
            Role = u.Role,
            Verified = u.Verified,
            IsActive = u.IsActive,
            Phone = u.Phone,
            ProfileImage = u.ProfileImage,
            Address = u.Address,
            GovtId = u.GovtId,
            GovtIdType = u.GovtIdType,
            CreatedAt = u.CreatedAt,
            UpdatedAt = u.UpdatedAt
        };

        public static ParkingSlotResponseDto ToSlotResponse(ParkingSlot s) => new()
        {
            Id = s.Id,
            Name = s.Name,
            Description = s.Description,
            Owner = s.Owner != null ? new OwnerInfoDto
            {
                Id = s.Owner.Id,
                Name = s.Owner.Name,
                Phone = s.Owner.Phone,
                Email = s.Owner.Email,
                ProfileImage = s.Owner.ProfileImage
            } : null,
            Location = new LocationDto
            {
                Address = s.Address,
                City = s.City,
                State = s.State,
                Pincode = s.Pincode,
                Latitude = s.Latitude,
                Longitude = s.Longitude
            },
            TotalSlots = s.TotalSlots,
            AvailableSlots = s.AvailableSlots,
            VehicleTypes = ParseJsonArray(s.VehicleTypesJson),
            PricePerHour = s.PricePerHour,
            PricePerDay = s.PricePerDay,
            PricePerMonth = s.PricePerMonth,
            Amenities = ParseJsonArray(s.AmenitiesJson),
            Photos = ParseJsonArray(s.PhotosJson),
            Rating = s.Rating,
            TotalReviews = s.TotalReviews,
            Reviews = s.Reviews.Select(r => new ReviewResponseDto
            {
                Id = r.Id,
                UserId = r.UserId,
                UserName = r.UserName,
                Comment = r.Comment,
                Rating = r.Rating,
                CreatedAt = r.CreatedAt
            }).ToList(),
            Status = s.Status,
            AutoApprove = s.AutoApprove,
            IsActive = s.IsActive,
            OpeningTime = s.OpeningTime,
            ClosingTime = s.ClosingTime,
            BlockedDates = s.BlockedDates.Select(b => b.Date).ToList(),
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt
        };

        public static BookingResponseDto ToBookingResponse(Booking b) => new()
        {
            Id = b.Id,
            User = b.User != null ? new UserSummaryDto
            {
                Id = b.User.Id,
                Name = b.User.Name,
                Email = b.User.Email,
                Phone = b.User.Phone
            } : null,
            ParkingSlot = b.ParkingSlot != null ? new ParkingSlotSummaryDto
            {
                Id = b.ParkingSlot.Id,
                Name = b.ParkingSlot.Name,
                Location = new LocationDto
                {
                    Address = b.ParkingSlot.Address,
                    City = b.ParkingSlot.City,
                    State = b.ParkingSlot.State,
                    Pincode = b.ParkingSlot.Pincode,
                    Latitude = b.ParkingSlot.Latitude,
                    Longitude = b.ParkingSlot.Longitude
                },
                PricePerHour = b.ParkingSlot.PricePerHour,
                Photos = ParseJsonArray(b.ParkingSlot.PhotosJson)
            } : null,
            SlotNumber = b.SlotNumber,
            VehicleRegNumber = b.VehicleRegNumber,
            VehicleType = b.VehicleType,
            StartDate = b.StartDate,
            EndDate = b.EndDate,
            Hours = b.Hours,
            TotalPrice = b.TotalPrice,
            Status = b.Status,
            PaymentStatus = b.PaymentStatus,
            PaymentMethod = b.PaymentMethod,
            PaymentId = b.PaymentId,
            PaymentGatewayOrderId = b.PaymentGatewayOrderId,
            QrCode = b.QrCode,
            Notes = b.Notes,
            CreatedAt = b.CreatedAt,
            UpdatedAt = b.UpdatedAt
        };

        public static List<string> ParseJsonArray(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        public static string ToJsonArray(IEnumerable<string>? list) =>
            JsonSerializer.Serialize(list ?? new List<string>());
    }
}
