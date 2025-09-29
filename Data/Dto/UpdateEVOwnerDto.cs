namespace EVChargingSystem.WebAPI.Data.Dtos
{
    public class UpdateEVOwnerDto
    {
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? VehicleModel { get; set; }
        public string? LicensePlate { get; set; }

        public string?Status { get; set; } // e.g., "Active", "Deactivated"
    }
}