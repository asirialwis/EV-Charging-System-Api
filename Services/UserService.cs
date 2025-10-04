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

        public async Task<(User? User, string? ErrorMessage)> AuthenticateAsync(string email, string password)
        {
            var user = await _userRepository.FindByEmailAndPasswordAsync(email, password);

            // Check 1: Invalid Credentials (Credentials failed)
            if (user == null)
            {
                return (null, "Invalid email or password.");
            }

            // Check 2: CRITICAL SECURITY CHECK (Account Status)
            if (user.Status == "Deactivated")
            {
                // Return null User and the specific error message
                return (null, "Account is currently deactivated. Please contact backoffice support.");
            }

            // 3. Authentication successful
            return (user, null);
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

        public async Task<bool> UpdateEVOwnerAsync(
           string nic,
           UpdateEVOwnerDto updateDto,
           string requestingUserId,
           string userRole)
        {
            var profile = await _profileRepository.FindByNicAsync(nic);
            if (profile == null) return false;

            // --- 1. Role-Based Access Check (Authorization) ---

            // Check if the requesting user is the profile owner or a Backoffice admin
            if (userRole == "EVOwner")
            {
                // EVOwner can only update their own profile (ownership check)
                if (profile.UserId.ToString() != requestingUserId) return false;
            }
            else if (userRole != "Backoffice")
            {
                // Only Backoffice or the EVOwner can perform this API call
                return false;
            }
            // Backoffice users are authorized to proceed.


            // --- 2. Dynamic PATCH Builder Logic ---
            var updateBuilder = Builders<EVOwnerProfile>.Update;
            var updates = new List<UpdateDefinition<EVOwnerProfile>>();

            // --- Profile Field Updates (Allowed for both Owner and Backoffice) ---
            if (updateDto.FullName != null)
            {
                updates.Add(updateBuilder.Set(p => p.FullName, updateDto.FullName));
            }
            // ... (Add all other profile fields here: Phone, Address, VehicleModel, LicensePlate) ...
            if (updateDto.Phone != null) updates.Add(updateBuilder.Set(p => p.Phone, updateDto.Phone));
            if (updateDto.Address != null) updates.Add(updateBuilder.Set(p => p.Address, updateDto.Address));
            if (updateDto.VehicleModel != null) updates.Add(updateBuilder.Set(p => p.VehicleModel, updateDto.VehicleModel));
            if (updateDto.LicensePlate != null) updates.Add(updateBuilder.Set(p => p.LicensePlate, updateDto.LicensePlate));


            // --- 3. Status Update Logic (Handles Complex Rules) ---
            if (updateDto.Status != null)
            {
                string newStatus = updateDto.Status;
                string currentStatus = profile.Status;

                // A. Strict Validation of Status Value
                if (newStatus != "Active" && newStatus != "Deactivated")
                {
                    return false; // Invalid status value provided
                }

                // B. Enforce Reactivation Rule: Only Backoffice can set status to "Active"
                if (newStatus == "Active")
                {
                    if (userRole != "Backoffice")
                    {
                        // Deactivated accounts can only be reactivated by a back-office officer
                        return false;
                    }
                }

                // C. Enforce Deactivation Rule: Owner can set status to "Deactivated"


                // D. Add the status update to the MongoDB update list
                updates.Add(updateBuilder.Set(p => p.Status, newStatus));

                // E. Cascade Update Logic (CRITICAL SECURITY STEP)
                // Update the corresponding User document to enable/disable login.
                var userUpdateSuccess = await _userRepository.UpdateStatusAsync(
                    profile.UserId.ToString(),
                    newStatus
                );

                if (!userUpdateSuccess)
                {
                    // Fail the transaction if the critical login status update fails
                    return false;
                }
            }


            // --- 4. Final Execution ---

            // Always update the UpdateAt timestamp
            updates.Add(updateBuilder.Set(p => p.UpdatedAt, DateTime.UtcNow));

            // Check if any profile fields were actually modified
            if (updates.Count == 0)
            {
                return true; // No data to update (just a check), operation is successful
            }

            // Combine all individual updates into one atomic operation
            var combinedUpdate = updateBuilder.Combine(updates);

            // 5. Save to repository using the new PartialUpdateAsync
            return await _profileRepository.PartialUpdateAsync(nic, combinedUpdate);
        }


        public async Task<EVOwnerProfileDto?> GetOwnerProfileAsync(string userId)
        {
            // 1. Fetch the Profile using the UserId from the JWT token
            var profile = await _profileRepository.FindByUserIdAsync(userId);
            if (profile == null)
            {
                // This scenario indicates a corrupt state (User exists, but no profile)
                return null;
            }

            // 2. Fetch the corresponding User document for the email
            var user = await _userRepository.FindByIdAsync(userId);
            if (user == null)
            {
                // Should not happen if data is consistent, but handles edge cases
                return null;
            }

            // 3. Map and return the combined DTO
            return new EVOwnerProfileDto
            {
                Nic = profile.Nic,
                Email = user.Email, // Joined from User document
                FullName = profile.FullName,
                Phone = profile.Phone,
                Address = profile.Address,
                VehicleModel = profile.VehicleModel,
                LicensePlate = profile.LicensePlate,
                Status = profile.Status,
                CreatedAt = profile.CreatedAt
            };
        }

    }
}
