using MongoDB.Bson;

namespace EVChargingSystem.WebAPI.Data.Dtos
{
    public class UpdateStationDto
    {
        // General Details
        public string? StationName { get; set; }
        public string? StationCode { get; set; }
        public string? AdditionalNotes { get; set; }
        public string? Status { get; set; } // For Activation/Deactivation

        // Capacity and Type
        public int? ACChargingSlots { get; set; }
        public int? DCChargingSlots { get; set; }
        public string? ACPowerOutput { get; set; }
        public string? ACConnector { get; set; }
        public string? ACChargingTime { get; set; }
        public int? TotalCapacity { get; set; }

        // Assignment Details (For Many-to-Many updates)
          public List<string> StationOperatorIds { get; set; } = new List<string>();

        // Location
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? Latitude { get; set; }
        public string? Longitude { get; set; }
        public string? GooglePlaceID { get; set; }
    }
}