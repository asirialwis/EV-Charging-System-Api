using System;
using MongoDB.Bson;
using EVChargingSystem.WebAPI.Data.Models; // Required for base model properties

namespace EVChargingSystem.WebAPI.Data.Dtos
{
    // Inherit from Booking model to fetch all its fields easily
    public class BookingDataDto : Booking 
    {
        // --- Joined EV Owner Details ---
        public string EVOwnerFullName { get; set; }
        public string EVOwnerNIC { get; set; }

        public string StationName { get; set; }
        public string StationCode { get; set; }
    }
}