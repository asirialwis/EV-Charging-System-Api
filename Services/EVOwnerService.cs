using EVChargingApi.Data.Dto;
using EVChargingApi.Data.Models;
using EVChargingApi.Data.Repositories;
using EVChargingSystem.WebAPI.Data.Repositories;

namespace EVChargingApi.Services
{
    public class EVOwnerService : IEVOwnerService
    {
        private readonly IEVOwnerProfileRepository _evOwnerProfileRepository;
        private readonly IUserRepository _userRepository;

        public EVOwnerService(IEVOwnerProfileRepository evOwnerProfileRepository, IUserRepository userRepository)
        {
            _evOwnerProfileRepository = evOwnerProfileRepository;
            _userRepository = userRepository;
        }

        public async Task<EVOwnerDetailsDto?> GetEVOwnerDetailsByNicAsync(string nic)
        {
            // Get EV owner profile by NIC
            var profile = await _evOwnerProfileRepository.FindByNicAsync(nic);
            if (profile == null)
            {
                return null;
            }

            // Get user details by UserId
            var user = await _userRepository.FindByIdAsync(profile.UserId.ToString());
            if (user == null)
            {
                return null;
            }

            // Map to DTO
            return new EVOwnerDetailsDto
            {
                Id = profile.Id,
                UserId = profile.UserId.ToString(),
                Nic = profile.Nic,
                Email = user.Email,
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
    }
}
