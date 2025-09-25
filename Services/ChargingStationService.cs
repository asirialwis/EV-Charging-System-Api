using EVChargingSystem.WebAPI.Data.Dtos;
using EVChargingSystem.WebAPI.Data.Models;
using EVChargingSystem.WebAPI.Data.Repositories;
using MongoDB.Bson;

namespace EVChargingSystem.WebAPI.Services
{
    public class ChargingStationService : IChargingStationService
    {
        private readonly IChargingStationRepository _stationRepository;

        public ChargingStationService(IChargingStationRepository stationRepository)
        {
            _stationRepository = stationRepository;
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
    }
}