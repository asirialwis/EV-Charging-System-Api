using System;

namespace EVChargingSystem.WebAPI.Data.Dtos
{
    public class OperationalUserDto
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Role { get; set; }
        public string Status { get; set; }
        public string AssignedStationId { get; set; }
        public string AssignedStationName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
