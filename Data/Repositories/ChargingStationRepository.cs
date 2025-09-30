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

        public ChargingStationRepository(MongoDbContext context)
        {
            _stations = context.GetCollection<ChargingStation>("ChargingStations");
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
            var projection = Builders<ChargingStation>.Projection.Include(s => s.StationOperatorId);

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
    }
}