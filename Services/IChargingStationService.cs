using EVChargingSystem.WebAPI.Data.Dtos;
using System.Threading.Tasks;

namespace EVChargingSystem.WebAPI.Services
{
    public interface IChargingStationService
    {
        Task CreateStationAsync(CreateStationDto stationDto);

        Task<List<OperatorDto>> GetUnassignedOperatorsAsync();

        Task<List<StationAssignmentDto>> GetAllStationsForAssignmentAsync();

        Task<bool> UpdateStationAsync(string stationId, UpdateStationDto updateDto);

        Task<List<StationWithBookingsDto>> GetStationsWithUpcomingBookingsAsync();
    }
}