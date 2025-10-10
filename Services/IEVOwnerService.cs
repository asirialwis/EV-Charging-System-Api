using EVChargingApi.Data.Dto;

namespace EVChargingApi.Services
{
    public interface IEVOwnerService
    {
        Task<EVOwnerDetailsDto?> GetEVOwnerDetailsByNicAsync(string nic);
    }
}
