using System;

namespace EVChargingSystem.WebAPI.Data.Dtos
{
    public class BookingResponseDto
    {
        public string Id { get; set; }
        public string EVOwnerId { get; set; }
        public string EVOwnerName { get; set; }
        public string EVOwnerNIC { get; set; }
        public string StationId { get; set; }
        public string StationName { get; set; }
        public string StationCode { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string SlotType { get; set; }
        public string SlotId { get; set; }
        public string Status { get; set; }
        public string? QrCodeBase64 { get; set; }
        public DateTime BookingDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
