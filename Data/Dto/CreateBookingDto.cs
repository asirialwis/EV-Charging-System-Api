namespace EVChargingSystem.WebAPI.Data.Dtos
{
    public class CreateBookingDto
    {
        // This comes from the JWT token, not the body
        public string EVOwnerId { get; set; } 
        
        public string StationId { get; set; }
        public string SlotType { get; set; }
        public string SlotId { get; set; }
        
        // The mobile app should send the full DateTime values
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}