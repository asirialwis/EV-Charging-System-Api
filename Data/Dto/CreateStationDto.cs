using MongoDB.Bson;

namespace EVChargingSystem.WebAPI.Data.Dtos
{
    public class CreateStationDto
    {
        public required string StationName { get; set; }
        public string? StationCode { get; set; }
        public int? ACChargingSlots { get; set; }
        public int? DCChargingSlots { get; set; }
        public string? ACPowerOutput { get; set; }
        public string? ACConnector { get; set; }
        public string? ACChargingTime { get; set; }
        public int? TotalCapacity { get; set; }
        public List<string> StationOperatorIds { get; set; } = new();
        public required string AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public required string City { get; set; }
        public required string Latitude { get; set; }
        public required string Longitude { get; set; }
        public string? GooglePlaceID { get; set; }
        public string? AdditionalNotes { get; set; }
        public string? Status { get; set; } // e.g., "Active", "Inactive", "Under Maintenance"
    }
}