using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace EVChargingSystem.WebAPI.Data.Models
{
    public class ChargingStation
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string StationName { get; set; }
        public string StationCode { get; set; }

        public int ACChargingSlots { get; set; }
        public int DCChargingSlots { get; set; }
        public string ACPowerOutput { get; set; }
        public string ACConnector { get; set; }
        public string ACChargingTime { get; set; }
        public int TotalCapacity { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId StationOperatorId { get; set; } // Link to the Station Operator user

        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public string GooglePlaceID { get; set; }
        public string AdditionalNotes { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}