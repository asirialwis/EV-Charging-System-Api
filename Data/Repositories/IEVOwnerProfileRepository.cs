using EVChargingApi.Data.Models;

namespace EVChargingApi.Data.Repositories
{
    public interface IEVOwnerProfileRepository
    {
        Task<EVOwnerProfile> FindByNicAsync(string nic);
        Task CreateAsync(EVOwnerProfile profile);
    }
}