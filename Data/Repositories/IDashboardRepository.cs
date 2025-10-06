using System.Threading.Tasks;
using EVChargingSystem.WebAPI.Data.Dtos;

namespace EVChargingSystem.WebAPI.Data.Repositories
{
    public interface IDashboardRepository
    {
        Task<long> CountReservationsByStatusAsync(string status, bool futureOnly);
        Task<long> CountStationsByStatusAsync(string status);
        Task<long> CountAllStationsAsync();
        Task<(long Used, long Total)> CalculateTodayCapacityAsync();
        Task<List<ChargingStationLocationDto>> GetActiveStationLocationsAsync();
    }
}