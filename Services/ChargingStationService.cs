using EVChargingSystem.WebAPI.Data.Dtos;
using EVChargingSystem.WebAPI.Data.Models;
using EVChargingSystem.WebAPI.Data.Repositories;
using MongoDB.Bson;

namespace EVChargingSystem.WebAPI.Services
{
    public class ChargingStationService : IChargingStationService
    {
        private readonly IChargingStationRepository _stationRepository;
        private readonly IUserRepository _userRepository;

        public ChargingStationService(IChargingStationRepository stationRepository, IUserRepository userRepository)
        {
            _stationRepository = stationRepository;
            _userRepository = userRepository;
        }


        public async Task CreateStationAsync(CreateStationDto stationDto)
        {
            var station = new ChargingStation
            {
                StationName = stationDto.StationName,
                StationCode = stationDto.StationCode ?? string.Empty,
                ACChargingSlots = stationDto.ACChargingSlots ?? 0,
                DCChargingSlots = stationDto.DCChargingSlots ?? 0,
                ACPowerOutput = stationDto.ACPowerOutput,
                ACConnector = stationDto.ACConnector ?? string.Empty,
                ACChargingTime = stationDto.ACChargingTime,
                TotalCapacity = stationDto.TotalCapacity ?? 0,
                StationOperatorId = new ObjectId(stationDto.StationOperatorId),
                AddressLine1 = stationDto.AddressLine1,
                AddressLine2 = stationDto.AddressLine2 ?? string.Empty,
                City = stationDto.City,
                Latitude = stationDto.Latitude,
                Longitude = stationDto.Longitude,
                GooglePlaceID = stationDto.GooglePlaceID ?? string.Empty,
                AdditionalNotes = stationDto.AdditionalNotes ?? string.Empty,
                Status = stationDto.Status ?? "Active", // Default to "Active" if not provided
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _stationRepository.CreateAsync(station);
        }


        public async Task<List<OperatorDto>> GetUnassignedOperatorsAsync()
        {
            // 1. Get all active Station Operators
            var allOperators = await _userRepository.GetUsersByRoleAsync("StationOperator");

            // 2. Get all assigned Operator IDs from the Stations collection
            var assignedIds = await _stationRepository.GetAllAssignedOperatorIdsAsync();

            // Convert assignedIds to a HashSet for fast lookup
            var assignedIdSet = new HashSet<string>(assignedIds);

            // 3. Filter in C# memory
            var unassignedOperators = allOperators
                .Where(op => !assignedIdSet.Contains(op.Id))
                .Select(op => new OperatorDto
                {
                    Id = op.Id,
                    Email = op.Email,
                    FullName = op.FullName
                })
                .ToList();

            return unassignedOperators;
        }
        

        public async Task<List<StationAssignmentDto>> GetAllStationsForAssignmentAsync()
        {
            // 1. Fetch ALL stations (requires GetAllStationsAsync in IChargingStationRepository)
            var allStations = await _stationRepository.GetAllStationsAsync();
            
            // 2. Map to the light DTO for the UI
            var assignmentList = allStations
                .Select(s => new StationAssignmentDto
                {
                    Id = s.Id,
                    StationName = s.StationName,
                    StationCode = s.StationCode
                })
                .ToList();

            return assignmentList;
        }
    }
}