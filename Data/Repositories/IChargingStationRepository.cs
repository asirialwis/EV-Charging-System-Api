using EVChargingSystem.WebAPI.Data.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Threading.Tasks;

namespace EVChargingSystem.WebAPI.Data.Repositories
{
    public interface IChargingStationRepository
    {
        Task CreateAsync(ChargingStation station);

        Task<ChargingStation> FindByIdAsync(ObjectId stationId);

        Task<List<string>> GetAllAssignedOperatorIdsAsync();

        Task<List<ChargingStation>> GetAllStationsAsync();
        
        Task<bool> PartialUpdateAsync(string stationId, UpdateDefinition<ChargingStation> updateDefinition);
    }
}