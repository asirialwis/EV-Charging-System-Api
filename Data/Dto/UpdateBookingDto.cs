using System;

namespace EVChargingSystem.WebAPI.Data.Dtos
{
    public class UpdateBookingDto
    {
        public string? StationId { get; set; }
        public string? SlotType { get; set; }
        public string? SlotId { get; set; }

        public string? Status { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }
}
