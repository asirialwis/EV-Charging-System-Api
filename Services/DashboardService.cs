using EVChargingSystem.WebAPI.Data.Dtos;
using EVChargingSystem.WebAPI.Data.Repositories;
using System.Threading.Tasks;

namespace EVChargingSystem.WebAPI.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IDashboardRepository _dashboardRepository;

        public DashboardService(IDashboardRepository dashboardRepository)
        {
            _dashboardRepository = dashboardRepository;
        }

        public async Task<DashboardMetricsDto> GetMetricsAsync()
        {
            // 1. Fetch Counts
            var pendingReservationsTask = _dashboardRepository.CountReservationsByStatusAsync("Pending", false);
            var approvedFutureReservationsTask = _dashboardRepository.CountReservationsByStatusAsync("Approved", true);
            var activeStationsCountTask = _dashboardRepository.CountStationsByStatusAsync("Active");
            var totalStationsCountTask = _dashboardRepository.CountAllStationsAsync();
            var capacityTask = _dashboardRepository.CalculateTodayCapacityAsync();

            // Run all tasks concurrently for optimal performance
            await Task.WhenAll(pendingReservationsTask, approvedFutureReservationsTask, activeStationsCountTask, totalStationsCountTask, capacityTask);

            // 2. Destructure Results
            long pendingReservations = pendingReservationsTask.Result;
            long approvedFutureReservations = approvedFutureReservationsTask.Result;
            long activeStationsCount = activeStationsCountTask.Result;
            long totalStationsCount = totalStationsCountTask.Result;
            var (usedSlots, totalSlots) = capacityTask.Result;

            // 3. Calculate Percentage
            double capacityPercentage = (totalSlots > 0) 
                ? Math.Round(((double)usedSlots / totalSlots) * 100, 2) 
                : 0.0;

            // 4. Return DTO
            return new DashboardMetricsDto
            {
                PendingReservations = (int)pendingReservations,
                ApprovedFutureReservations = (int)approvedFutureReservations,
                ActiveStationsCount = (int)activeStationsCount,
                TotalStationsCount = (int)totalStationsCount,
                TodayCapacityUsedSlots = (int)usedSlots,
                TodayCapacityTotalSlots = (int)totalSlots,
                TodayCapacityPercentage = capacityPercentage
            };
        }
    }
}