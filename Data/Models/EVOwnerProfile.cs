using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace EVChargingApi.Data.Models
{
    public class EVOwnerProfile
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId UserId { get; set; } // Link to the User document
        public string Nic { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string VehicleModel { get; set; }
        public string LicensePlate { get; set; }

        //account can be activated/deactivated by Backoffice user.
        public string Status { get; set; } // e.g., "Active", "Deactivated"

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}