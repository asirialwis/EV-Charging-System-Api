using System.Collections.Generic;

namespace EVChargingSystem.WebAPI.Data.Dtos
{
    public class AvailabilityResponseDto
    {
        public bool IsAvailable { get; set; }
        public List<string> AvailableSlotIds { get; set; } = new List<string>();
        public string Message { get; set; }
    }
}
