using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace EVChargingSystem.WebAPI.Data.Models
{
    public class Booking
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId EVOwnerId { get; set; } // The user making the booking

        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId StationId { get; set; } // The station being booked

        public required string SlotType { get; set; }

        public required string SlotId { get; set; } // The specific slot ID being booked (e.g., A1, A2, D1, D2)

        // Use DateTime for precise booking management and consistency
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public string Status { get; set; } // e.g., "Pending", "Approved", "Canceled", "Completed"

        public string QrCodeBase64 { get; set; }
         
        public DateTime BookingDate { get; set; }
        public DateTime CreatedAt { get; set; }
        
        public DateTime UpdatedAt { get; set; }
    }
}