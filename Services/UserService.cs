using EVChargingApi.Services;
using EVChargingSystem.WebAPI.Data.Repositories;
using EVChargingApi.Data.Models;
using EVChargingApi.Dto;
using EVChargingApi.Data.Repositories;
using EVChargingSystem.WebAPI.Data.Dtos;
using MongoDB.Driver;
using MongoDB.Bson;
using EVChargingSystem.WebAPI.Utils;

namespace EVChargingSystem.WebAPI.Services
{
    public class UserService : IUserService
    {

        private readonly IUserRepository _userRepository;
        private readonly IEVOwnerProfileRepository _profileRepository;
        private readonly IChargingStationRepository _stationRepository;
        private readonly IEmailService _emailService;

        public UserService(IUserRepository userRepository, IEVOwnerProfileRepository profileRepository, IChargingStationRepository stationRepository, IEmailService emailService)
        {
            _userRepository = userRepository;
            _profileRepository = profileRepository;
            _stationRepository = stationRepository;
            _emailService = emailService;
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
                CreatedAt = profile.CreatedAt,
                UpdatedAt = profile.UpdatedAt
            };
        }


        public async Task<List<EVOwnerProfileDto>> GetAllEVOwnersAsync()
        {
            // 1. Fetch all profiles (contains NIC, UserId, FullName, Status, etc.)
            var allProfiles = await _profileRepository.GetAllProfilesAsync();

            // 2. Collect all unique User IDs needed for the join
            var userIds = allProfiles.Select(p => p.UserId.ToString()).Distinct().ToList();

            // 3. Fetch all corresponding User documents (contains Email, Status) in ONE batch query
            var allUsers = await _userRepository.FindManyByIdsAsync(userIds);

            // 4. Create a fast lookup dictionary (key: UserId)
            var userLookup = allUsers.ToDictionary(u => u.Id, u => u);

            // 5. Perform the in-memory join and mapping
            var result = allProfiles.Select(profile =>
            {
                // Try to find the corresponding User document
                userLookup.TryGetValue(profile.UserId.ToString(), out var user);

                return new EVOwnerProfileDto
                {
                    Nic = profile.Nic,
                    Email = user?.Email ?? "N/A", 
                    FullName = profile.FullName,
                    Phone = profile.Phone,
                    Address = profile.Address,
                    VehicleModel = profile.VehicleModel,
                    LicensePlate = profile.LicensePlate,
                    Status = profile.Status,
                    CreatedAt = profile.CreatedAt
                };
            }).ToList();

            return result;
        }




        public async Task<(bool Success, string Message)> CreateOperatorAndAssignStationsAsync(CreateOperationalUserDto userDto)
        {
            // 1. Create the new User (Operator/Backoffice)
            var user = new User
            {
                Email = userDto.Email,
                Password = userDto.Password,
                Role = userDto.Role,
                FullName = userDto.FullName,
                Phone = userDto.Phone,
                Status = "Active",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Note: The User.Id field is populated by the MongoDB driver during CreateAsync
            await _userRepository.CreateAsync(user);

            // 2. Assign Station(s) (Only applies to Station Operators)
            if (user.Role == "StationOperator" && userDto.AssignedStations.Any())
            {
                // Add the newly created User ID to the selected Station(s)
                var assignmentSuccess = await _stationRepository.AddOperatorToStationsAsync(
                    userDto.AssignedStations,
                    user.Id // The new ID generated by MongoDB
                );

                // CRITICAL CHECK: If station assignment fails, log a warning but don't fail user creation, 
                // as the User account itself is valid. Admin must re-assign later.
                if (!assignmentSuccess)
                {
                    return (true, "User created successfully, but station assignment failed. Please assign manually.");
                }
            }

            return (true, $"{user.Role} created successfully.");
        }


        public async Task<(bool Success, string Message)> DeleteEVOwnerAsync(string nic)
        {
            // 1. Find the profile first (to get the associated UserId)
            var profile = await _profileRepository.FindByNicAsync(nic);
            if (profile == null)
            {
                return (false, "EV Owner profile not found.");
            }

            // 2. Delete the profile document (using NIC as PK)
            var profileDeleteSuccess = await _profileRepository.DeleteAsync(nic);

            // 3. Delete the linked User document (using UserId)
            var userDeleteSuccess = await _userRepository.DeleteAsync(profile.UserId.ToString());

            // 4. Final check and response
            if (profileDeleteSuccess && userDeleteSuccess)
            {

                return (true, "EV Owner account and profile successfully deleted.");
            }
            else
            {
                // Handle partial failure (e.g., log error and alert admin)
                return (false, "Deletion failed for one or more linked documents.");
            }
        }




        public async Task<(bool Success, string Message)> CreateOwnerByAdminAsync(AdminCreateEVOwnerDto ownerDto)
        {
            // 1. Validation Checks (Email and NIC uniqueness)
            var existingUser = await _userRepository.FindByEmailAsync(ownerDto.Email);
            if (existingUser != null) return (false, "Registration failed. Email already exists.");

            var existingProfile = await _profileRepository.FindByNicAsync(ownerDto.Nic);
            if (existingProfile != null) return (false, "Registration failed. NIC already exists.");

            // 2. Generate Secure Password
            string tempPassword = PasswordGenerator.GenerateTemporaryPassword();

            // 3. Create the User document (using the generated password)
            var user = new User
            {
                Email = ownerDto.Email,
                Password = tempPassword, // Store the generated password
                Role = "EVOwner",
                Status = "Active",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _userRepository.CreateAsync(user); // User.Id is populated here

            // 4. Create the EVOwnerProfile document
            var profile = new EVOwnerProfile
            {
                UserId = new ObjectId(user.Id),
                Nic = ownerDto.Nic,
                FullName = ownerDto.FullName,
                Phone = ownerDto.Phone,
                Address = ownerDto.Address,
                VehicleModel = ownerDto.VehicleModel,
                LicensePlate = ownerDto.LicensePlate,
                Status = "Active",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _profileRepository.CreateAsync(profile);

            // 5. CRITICAL: Email the temporary password
            try
            {

                await _emailService.SendTemporaryPasswordAsync(ownerDto.Email, tempPassword);


                return (true, "Account created successfully and notification emailed.");
            }
            catch (MailKit.Security.AuthenticationException ex)
            {
                // Log the specific authentication failure details for debugging
                Console.WriteLine($"SMTP Authentication Failed: {ex.Message}");
                // Return a clean success message for the account creation
                return (true, "Account created, but email notification failed. Password must be manually given.");
            }
            catch (Exception ex)
            {
                // Log general connection/protocol errors
                Console.WriteLine($"Email sending failed: {ex.GetType().Name} - {ex.Message}");
                return (true, "Account created, but email notification failed. Password must be manually given.");
            }

            return (true, "Account created. Temporary password emailed to EV Owner.");
        }

    }
}
