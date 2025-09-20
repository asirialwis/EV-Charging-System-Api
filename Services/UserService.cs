using EVChargingApi.Services;
using EVChargingSystem.WebAPI.Data.Repositories;
using EVChargingApi.Data.Models;
using EVChargingApi.Dto;
using EVChargingApi.Data.Repositories;

namespace EVChargingSystem.WebAPI.Services
{
    public class UserService : IUserService
    {
        
        private readonly IUserRepository _userRepository;
        private readonly IEVOwnerProfileRepository _profileRepository;

        public UserService(IUserRepository userRepository, IEVOwnerProfileRepository profileRepository)
        {
            _userRepository = userRepository;
            _profileRepository = profileRepository;
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



        // method to handle the complete EV Owner registration flow
        public async Task<bool> RegisterEVOwnerAsync(RegisterUserDto userDto)
        {
            // 1. Check if email already exists
            var existingUser = await _userRepository.FindByEmailAsync(userDto.Email);
            if (existingUser != null) return false;

            // 2. Check if NIC already exists
            var existingProfile = await _profileRepository.FindByNicAsync(userDto.Nic);
            if (existingProfile != null) return false;

            // 3. Create the User document
            var user = new User
            {
                Email = userDto.Email,
                Password = userDto.Password,
                Role = "EVOwner"
            };
            await _userRepository.CreateAsync(user);

            // 4. Create the EVOwnerProfile document
            var profile = new EVOwnerProfile
            {
                UserId = new MongoDB.Bson.ObjectId(user.Id), // Link the profile to the new user's ID
                Nic = userDto.Nic,
                FullName = userDto.FullName,
                Phone = userDto.Phone,
                Address = userDto.Address,
                VehicleModel = userDto.VehicleModel,
                LicensePlate = userDto.LicensePlate,
                Status = "Active", // accounts are created as Active 
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _profileRepository.CreateAsync(profile);

            return true;
        }
    }
}