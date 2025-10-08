using EVChargingApi.Data.Models;
using EVChargingApi.Dto;
using EVChargingSystem.WebAPI.Data.Dtos;

namespace EVChargingApi.Services
{
    public interface IUserService
    {
        Task<(User? User, string? ErrorMessage, string? AssignedStationId, string? AssignedStationName)> AuthenticateAsync(string email, string password);
        Task CreateAsync(User user);

        Task<bool> RegisterEVOwnerAsync(RegisterUserDto userDto);

        Task<bool> UpdateEVOwnerAsync(string nic, UpdateEVOwnerDto updateDto, string requestingUserId, string userRole);

        Task<EVOwnerProfileDto?> GetOwnerProfileAsync(string userId);
        Task<List<EVOwnerProfileDto>> GetAllEVOwnersAsync();

        Task<(bool Success, string Message)> CreateOperatorAndAssignStationsAsync(CreateOperationalUserDto userDto);
        Task<(bool Success, string Message)> DeleteEVOwnerAsync(string nic);

        Task<(bool Success, string Message)> CreateOwnerByAdminAsync(AdminCreateEVOwnerDto ownerDto);

    }
}