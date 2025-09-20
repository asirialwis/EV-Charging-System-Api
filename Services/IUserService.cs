using EVChargingApi.Data.Models;
using EVChargingApi.Dto;

namespace EVChargingApi.Services
{
    public interface IUserService
    {
        Task<User> AuthenticateAsync(string email, string password);
        Task CreateAsync(User user);

        Task<bool> RegisterEVOwnerAsync(RegisterUserDto userDto);

    }
}