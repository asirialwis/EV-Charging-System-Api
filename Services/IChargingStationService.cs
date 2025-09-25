using EVChargingSystem.WebAPI.Data.Dtos;
using System.Threading.Tasks;

namespace EVChargingSystem.WebAPI.Services
{
    public interface IChargingStationService
    {
        Task CreateStationAsync(CreateStationDto stationDto);
    }
}