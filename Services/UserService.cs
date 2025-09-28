using EVChargingApi.Services;
using EVChargingSystem.WebAPI.Data.Repositories;
using EVChargingApi.Data.Models;
using EVChargingApi.Dto;
using EVChargingApi.Data.Repositories;
using EVChargingSystem.WebAPI.Data.Dtos;
using MongoDB.Driver;

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

        public async Task<bool> UpdateEVOwnerAsync(string nic, UpdateEVOwnerDto updateDto, string requestingUserId, string userRole)
        {
            var profile = await _profileRepository.FindByNicAsync(nic);
            if (profile == null) return false;

            // --- 1. Role-Based Access Check ---
            // (This logic remains the same to enforce authorization)
            if (userRole == "EVOwner")
            {
                if (profile.UserId.ToString() != requestingUserId) return false;
            }
            else if (userRole != "Backoffice")
            {
                return false;
            }

            // --- 2. Dynamic PATCH Builder Logic ---
            var updateBuilder = Builders<EVOwnerProfile>.Update;
            var updates = new List<UpdateDefinition<EVOwnerProfile>>();

            // Use reflection or explicit checks to add only non-null properties to the update list
            if (updateDto.FullName != null)
            {
                updates.Add(updateBuilder.Set(p => p.FullName, updateDto.FullName));
            }
            if (updateDto.Phone != null)
            {
                updates.Add(updateBuilder.Set(p => p.Phone, updateDto.Phone));
            }
            if (updateDto.Address != null)
            {
                updates.Add(updateBuilder.Set(p => p.Address, updateDto.Address));
            }
            if (updateDto.VehicleModel != null)
            {
                updates.Add(updateBuilder.Set(p => p.VehicleModel, updateDto.VehicleModel));
            }
            if (updateDto.LicensePlate != null)
            {
                updates.Add(updateBuilder.Set(p => p.LicensePlate, updateDto.LicensePlate));
            }

            // ---Status Update Logic (Backoffice Only) ---
            if (updateDto.Status != null)
            {
                // A. Strict Validation of Status Value
                if (updateDto.Status != "Active" && updateDto.Status != "Deactivated")
                {
                    return false; // Invalid status value provided
                }

                // B. Role Enforcement: Only Backoffice can change status
                if (userRole != "Backoffice")
                {
                    // Fail the entire operation if a non-Backoffice user tries to change Status
                    return false;
                }

                // C. Add the status update to the MongoDB update list
                updates.Add(updateBuilder.Set(p => p.Status, updateDto.Status));

                // FUTURE STEP: When deactivating, the corresponding 'User' document should 
                // also be updated (e.g., set a 'CanLogin' flag or change their role/status) 
                // to prevent them from logging in via the AuthController.
            }


            // Always update the UpdateAt timestamp
            updates.Add(updateBuilder.Set(p => p.UpdatedAt, DateTime.UtcNow));

            // Check if any fields were actually updated
            if (updates.Count == 0)
            {
                return true; // No data to update, but the operation is considered successful
            }

            // Combine all individual updates into one atomic operation
            var combinedUpdate = updateBuilder.Combine(updates);

            // 3. Save to repository using the new PartialUpdateAsync
            return await _profileRepository.PartialUpdateAsync(nic, combinedUpdate);
        }
    }
}