using MongoDB.Driver;
using EVChargingSystem.WebAPI.Data.Models;
using EVChargingSystem.WebAPI.Data;
using System.Threading.Tasks;

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
    }
}