using MongoDB.Driver;
using EVChargingSystem.WebAPI.Data;

using EVChargingApi.Services;
using EVChargingSystem.WebAPI.Data.Repositories;
using EVChargingApi.Data.Models;

namespace EVChargingSystem.WebAPI.Services
{
    public class UserService : IUserService
    {
        // Change from IMongoCollection to IUserRepository
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<User> AuthenticateAsync(string email, string password)
        {
            //service uses the repository method
            var user = await _userRepository.FindByEmailAndPasswordAsync(email, password);
            return user;
        }

        public async Task CreateAsync(User user)
        {
            // The service calls the repository's create method
            await _userRepository.CreateAsync(user);
        }
    }
}