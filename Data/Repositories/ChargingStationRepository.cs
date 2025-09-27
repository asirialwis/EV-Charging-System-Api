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
    }
}