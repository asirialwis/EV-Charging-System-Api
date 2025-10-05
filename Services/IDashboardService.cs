using EVChargingSystem.WebAPI.Data.Dtos;
using System.Threading.Tasks;

namespace EVChargingSystem.WebAPI.Services
{
    public interface IDashboardService
    {
        Task<DashboardMetricsDto> GetMetricsAsync();
    }
}