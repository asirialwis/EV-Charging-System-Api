namespace EVChargingSystem.WebAPI.Data.Dtos
{
    public class ChargingStationLocationDto
    {
        public string StationId { get; set; }
        public string StationName { get; set; }
        public string StationCode { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Status { get; set; }
        public string TotalCapacity { get; set; } // Optional detail to display on marker popup
    }
}