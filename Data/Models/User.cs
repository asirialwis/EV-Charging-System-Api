using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EVChargingApi.Data.Models
{

    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Email { get; set; }
        public string Password { get; set; }
        public string Role { get; set; } // "Backoffice", "StationOperator", "EVOwner"

        public string FullName { get; set; }

        public string Phone { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public List<string> AssignedStations { get; set; } = new List<string>(); // For Station Operators, link to their station
    }
}