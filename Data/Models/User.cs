using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EVChargingApi.Data.Models
{

    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
       
        public required string Email { get; set; }
        public required string Password { get; set; }
        public required string Role { get; set; }
        public required string FullName { get; set; }
        public string? Phone { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string? AssignedStationId { get; set; } 
        public string? Status { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}