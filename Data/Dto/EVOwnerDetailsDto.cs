using System;

namespace EVChargingApi.Data.Dto
{
    public class EVOwnerDetailsDto
    {
        public string? Id { get; set; }
        public string? UserId { get; set; }
        public string? Nic { get; set; }
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? VehicleModel { get; set; }
        public string? LicensePlate { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
