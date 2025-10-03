using EVChargingSystem.WebAPI.Data.Dtos;
using EVChargingSystem.WebAPI.Data.Models;
using EVChargingSystem.WebAPI.Data.Repositories;
using MongoDB.Bson;
using MongoDB.Driver;

namespace EVChargingSystem.WebAPI.Services
{
    public class ChargingStationService : IChargingStationService
    {
        private readonly IChargingStationRepository _stationRepository;
        private readonly IUserRepository _userRepository;
        private readonly IBookingRepository _bookingRepository; // For booking checks during deactivation`

        public ChargingStationService(IChargingStationRepository stationRepository, IUserRepository userRepository, IBookingRepository bookingRepository)
        {
            _stationRepository = stationRepository;
            _userRepository = userRepository;
            _bookingRepository = bookingRepository; // For booking checks during deactivation
        }


        public async Task CreateStationAsync(CreateStationDto stationDto)
        {
            var acSlotCount = stationDto.ACChargingSlots ?? 0;
            var dcSlotCount = stationDto.DCChargingSlots ?? 0;

            var station = new ChargingStation
            {
                StationName = stationDto.StationName,
                StationCode = stationDto.StationCode ?? string.Empty,
                ACChargingSlots = acSlotCount,
                DCChargingSlots = dcSlotCount,
                // Generate slot ID arrays based on slot counts
                ACSlots = GenerateACSlotArray(acSlotCount),
                DCSlots = GenerateDCSlotArray(dcSlotCount),
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

        // Helper methods to generate slot ID arrays
        private List<string> GenerateACSlotArray(int count)
        {
            if (count <= 0) return new List<string>();
            return Enumerable.Range(1, count).Select(i => $"A{i}").ToList();
        }

        private List<string> GenerateDCSlotArray(int count)
        {
            if (count <= 0) return new List<string>();
            return Enumerable.Range(1, count).Select(i => $"D{i}").ToList();
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




        public async Task<bool> UpdateStationAsync(string stationId, UpdateStationDto updateDto)
        {
            if (!ObjectId.TryParse(stationId, out var id)) return false;

            // Check Assignment Requirement: Deactivating stations
            if (updateDto.Status == "Deactivated")
            {
                // MUST add a check here: "cannot deactivate if active bookings exist"
                // This requires an IBookingRepository method to check for active/pending bookings
                var hasActiveBookings = await _bookingRepository.HasActiveBookingsForStationAsync(id); // ASSUMED METHOD
                if (hasActiveBookings)
                {
                    // Fail the update if active bookings are found
                    return false;
                }
            }

            // --- Dynamic PATCH Builder Logic ---
            var updateBuilder = Builders<ChargingStation>.Update;
            var updates = new List<UpdateDefinition<ChargingStation>>();

            // MAPPING LOGIC: Check for null and add to updates list
            if (updateDto.StationName != null) updates.Add(updateBuilder.Set(s => s.StationName, updateDto.StationName));
            if (updateDto.StationCode != null) updates.Add(updateBuilder.Set(s => s.StationCode, updateDto.StationCode));
            if (updateDto.AdditionalNotes != null) updates.Add(updateBuilder.Set(s => s.AdditionalNotes, updateDto.AdditionalNotes));
            if (updateDto.Status != null) updates.Add(updateBuilder.Set(s => s.Status, updateDto.Status));

            // Numeric fields must check if they have a value (e.g., != null)
            if (updateDto.ACChargingSlots.HasValue)
            {
                updates.Add(updateBuilder.Set(s => s.ACChargingSlots, updateDto.ACChargingSlots.Value));
                // Regenerate AC slot array when count changes
                updates.Add(updateBuilder.Set(s => s.ACSlots, GenerateACSlotArray(updateDto.ACChargingSlots.Value)));
            }
            if (updateDto.DCChargingSlots.HasValue)
            {
                updates.Add(updateBuilder.Set(s => s.DCChargingSlots, updateDto.DCChargingSlots.Value));
                // Regenerate DC slot array when count changes
                updates.Add(updateBuilder.Set(s => s.DCSlots, GenerateDCSlotArray(updateDto.DCChargingSlots.Value)));
            }
            if (updateDto.TotalCapacity.HasValue) updates.Add(updateBuilder.Set(s => s.TotalCapacity, updateDto.TotalCapacity.Value));

            // String fields
            if (updateDto.ACPowerOutput != null) updates.Add(updateBuilder.Set(s => s.ACPowerOutput, updateDto.ACPowerOutput));
            if (updateDto.ACConnector != null) updates.Add(updateBuilder.Set(s => s.ACConnector, updateDto.ACConnector));
            if (updateDto.ACChargingTime != null) updates.Add(updateBuilder.Set(s => s.ACChargingTime, updateDto.ACChargingTime));

            // Location/Address
            if (updateDto.AddressLine1 != null) updates.Add(updateBuilder.Set(s => s.AddressLine1, updateDto.AddressLine1));
            if (updateDto.AddressLine2 != null) updates.Add(updateBuilder.Set(s => s.AddressLine2, updateDto.AddressLine2));
            if (updateDto.City != null) updates.Add(updateBuilder.Set(s => s.City, updateDto.City));
            if (updateDto.Latitude != null) updates.Add(updateBuilder.Set(s => s.Latitude, updateDto.Latitude));
            if (updateDto.Longitude != null) updates.Add(updateBuilder.Set(s => s.Longitude, updateDto.Longitude));
            if (updateDto.GooglePlaceID != null) updates.Add(updateBuilder.Set(s => s.GooglePlaceID, updateDto.GooglePlaceID));

            // Station Operator Assignment Update (Requires conversion if not null)
            if (updateDto.StationOperatorId != null)
            {
                // NOTE: For many-to-many, this should be an array update. 
                // Assuming single assignment for now, but converting string to ObjectId
                updates.Add(updateBuilder.Set(s => s.StationOperatorId, new ObjectId(updateDto.StationOperatorId)));
            }


            // Always update the UpdateAt timestamp
            updates.Add(updateBuilder.Set(s => s.UpdatedAt, DateTime.Now));

            // Final Check and Execution
            if (updates.Count == 0) return true; // Nothing to update

            var combinedUpdate = updateBuilder.Combine(updates);
            return await _stationRepository.PartialUpdateAsync(stationId, combinedUpdate);
        }

        public async Task<List<StationWithBookingsDto>> GetStationsWithUpcomingBookingsAsync()
        {
            // IST/SLST Offset
            TimeSpan istOffset = TimeSpan.FromHours(5.5);

            // 1. Fetch ALL stations
            var allStations = await _stationRepository.GetAllStationsAsync();
            var stationObjectIds = allStations.Select(s => new ObjectId(s.Id)).ToList();

            // 2. Fetch ALL relevant upcoming bookings in ONE query
            var allUpcomingBookings = await _stationRepository.GetUpcomingBookingsByStationIdsAsync(stationObjectIds, 0);

            // 3. Group bookings by Station ID for efficient lookup
            var bookingsLookup = allUpcomingBookings
                .GroupBy(b => b.StationId.ToString())
                .ToDictionary(g => g.Key, g => g.OrderBy(b => b.StartTime).ToList());

            // 4. Map Stations, apply the "MAX 2 bookings" rule, and perform UTC conversion
            var result = allStations.Select(station =>
            {
                var stationDto = new StationWithBookingsDto
                {
                    Id = station.Id,
                    StationName = station.StationName,
                    StationCode = station.StationCode,
                    ACChargingSlots = station.ACChargingSlots,
                    DCChargingSlots = station.DCChargingSlots,
                    ACSlots = station.ACSlots,
                    DCSlots = station.DCSlots,
                    ACPowerOutput = station.ACPowerOutput,
                    ACConnector = station.ACConnector,
                    ACChargingTime = station.ACChargingTime,
                    AddressLine1 = station.AddressLine1,
                    AddressLine2 = station.AddressLine2,
                    City = station.City,
                    Latitude = station.Latitude,
                    Longitude = station.Longitude,
                    TotalCapacity = station.TotalCapacity,
                    Status = station.Status,
                    AdditionalNotes = station.AdditionalNotes,
                };


                if (bookingsLookup.TryGetValue(station.Id, out var stationBookings))
                {
                    stationDto.UpcomingBookings = stationBookings
                        .Take(2) // APPLYING THE MAX 2 LIMIT HERE
                        .Select(b =>
                        {
                           
                            var localStartTime = b.StartTime.Add(istOffset);
                            var localEndTime = b.EndTime.Add(istOffset);

                            return new SimpleBookingDto
                            {
                                BookingId = b.Id,
                                StartTimeLocal = localStartTime,
                                EndTimeLocal = localEndTime,
                                SlotType = b.SlotType
                            };
                        })
                        .ToList();
                }

                return stationDto;
            }).ToList();

            return result;
        }


    }
}