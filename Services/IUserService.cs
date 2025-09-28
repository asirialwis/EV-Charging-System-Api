using EVChargingApi.Data.Models;
using EVChargingApi.Dto;
using EVChargingSystem.WebAPI.Data.Dtos;

namespace EVChargingApi.Services
{
    public interface IUserService
    {
        Task<User> AuthenticateAsync(string email, string password);
        Task CreateAsync(User user);

        Task<bool> RegisterEVOwnerAsync(RegisterUserDto userDto);

        Task<bool> UpdateEVOwnerAsync(string nic, UpdateEVOwnerDto updateDto, string requestingUserId, string userRole);

    }
}