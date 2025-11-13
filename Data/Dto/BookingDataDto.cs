using System;
using MongoDB.Bson;
using EVChargingSystem.WebAPI.Data.Models; // Required for base model properties

namespace EVChargingSystem.WebAPI.Data.Dtos
{
    // Inherit from Booking model to fetch all its fields easily
    public class BookingDataDto
    {
          public string Id { get; set; } // The Booking _id
        public string EVOwnerId { get; set; } // The simple string ID of the EV Owner Profile
        public string StationId { get; set; } // The simple string ID of the Charging Station
        
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string SlotType { get; set; }
        public string SlotId { get; set; }
        public string Status { get; set; }
        public DateTime BookingDate { get; set; }
        public string QrCodeBase64 { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // --- Joined Data ---
        public string EVOwnerFullName { get; set; }
        public string EVOwnerNIC { get; set; }
        public string StationName { get; set; }
        public string StationCode { get; set; }
    }
}