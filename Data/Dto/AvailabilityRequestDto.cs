using System;

namespace EVChargingSystem.WebAPI.Data.Dtos
{
    public class AvailabilityRequestDto
    {
        public string StationId { get; set; }
        public string SlotType { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}
