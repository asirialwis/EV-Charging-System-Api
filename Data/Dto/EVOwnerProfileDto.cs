using MongoDB.Bson;
using System;

namespace EVChargingSystem.WebAPI.Data.Dtos
{
    public class EVOwnerProfileDto
    {
        public string Nic { get; set; } // Primary key
        public string Email { get; set; } // From Users collection
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string VehicleModel { get; set; }
        public string LicensePlate { get; set; }
        public string Status { get; set; } // Active/Deactivated
        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}