using MongoDB.Driver;
using EVChargingSystem.WebAPI.Data.Models;
using EVChargingSystem.WebAPI.Data;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace EVChargingSystem.WebAPI.Data.Repositories
{
    public class ChargingStationRepository : IChargingStationRepository
    {
        private readonly IMongoCollection<ChargingStation> _stations;
        private readonly IMongoCollection<Booking> _bookings;

        public ChargingStationRepository(MongoDbContext context)
        {
            _stations = context.GetCollection<ChargingStation>("ChargingStations");
            _bookings = context.GetCollection<Booking>("Bookings");
        }

        public async Task CreateAsync(ChargingStation station)
        {
            await _stations.InsertOneAsync(station);
        }

        public async Task<ChargingStation> FindByIdAsync(ObjectId stationId)
        {
            // Implementation to find the station by its ObjectId
            return await _stations.Find(s => s.Id == stationId.ToString()).FirstOrDefaultAsync();
        }

        public async Task<List<string>> GetAllAssignedOperatorIdsAsync()
        {
            // Projection to include only the StationOperatorId field
            var projection = Builders<ChargingStation>.Projection.Include(s => s.StationOperatorIds);

            var assignedDocs = await _stations
                .Find(_ => true) // Find all stations
                .Project<BsonDocument>(projection)
                .ToListAsync();

            //  MAPPING LOGIC:
            return assignedDocs
                .Where(doc => doc.Contains("StationOperatorId"))
                .Select(doc =>
                {
                    // 1. Get the value as a BsonObjectId
                    var objectId = doc["StationOperatorId"].AsObjectId;
                    // 2. Convert the ObjectId instance to a string
                    return objectId.ToString();
                })
                .ToList();
        }

        public async Task<List<ChargingStation>> GetAllStationsAsync()
        {
            // Implementation to find ALL documents
            return await _stations.Find(_ => true).ToListAsync();
        }

        public async Task<bool> PartialUpdateAsync(string stationId, UpdateDefinition<ChargingStation> updateDefinition)
        {
            var filter = Builders<ChargingStation>.Filter.Eq(s => s.Id, stationId);

            // UpdateOneAsync performs the partial update
            var result = await _stations.UpdateOneAsync(filter, updateDefinition);

            return result.ModifiedCount == 1;
        }

         public async Task<List<Booking>> GetUpcomingBookingsByStationIdsAsync(List<ObjectId> stationIds, int limitPerStation)
    {
        var filter = Builders<Booking>.Filter.In(b => b.StationId, stationIds) &
                     // Status must be active/pending and StartTime must be in the future
                     Builders<Booking>.Filter.Gt(b => b.StartTime, DateTime.UtcNow) &
                     Builders<Booking>.Filter.Ne(b => b.Status, "Canceled") &
                     Builders<Booking>.Filter.Ne(b => b.Status, "Completed");

        // Fetch all future, relevant bookings, sorted by start time
        var pipeline = _bookings.Find(filter).SortBy(b => b.StartTime);

        return await pipeline.ToListAsync();
    }
    }
}