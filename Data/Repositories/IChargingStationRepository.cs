using EVChargingSystem.WebAPI.Data.Models;
using System.Threading.Tasks;

namespace EVChargingSystem.WebAPI.Data.Repositories
{
    public interface IChargingStationRepository
    {
        Task CreateAsync(ChargingStation station);
    }
}