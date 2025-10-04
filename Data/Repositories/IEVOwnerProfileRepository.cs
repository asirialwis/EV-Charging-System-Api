using EVChargingApi.Data.Models;
using MongoDB.Driver;

namespace EVChargingApi.Data.Repositories
{
    public interface IEVOwnerProfileRepository
    {
        Task<EVOwnerProfile> FindByNicAsync(string nic);
        Task CreateAsync(EVOwnerProfile profile);

        Task<bool> PartialUpdateAsync(string nic, UpdateDefinition<EVOwnerProfile> updateDefinition);
        Task<EVOwnerProfile> FindByUserIdAsync(string userId);
        Task<List<EVOwnerProfile>> GetAllProfilesAsync(); 
    }
}