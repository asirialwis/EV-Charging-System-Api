//services for charging station. crud operations and other business logic
using EVChargingApi.Data.Repositories;
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
        private readonly IEVOwnerProfileRepository _profileRepository;


        public ChargingStationService(IChargingStationRepository stationRepository, IUserRepository userRepository, IBookingRepository bookingRepository, IEVOwnerProfileRepository profileRepository)
        {
            _stationRepository = stationRepository;
            _userRepository = userRepository;
            _bookingRepository = bookingRepository;
            _profileRepository = profileRepository;
        }

        // Create a new charging station
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
                StationOperatorIds = stationDto.StationOperatorIds.Select(id => ObjectId.Parse(id)).ToList(),
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
        // Helper methods to generate slot ID arrays
        private List<string> GenerateDCSlotArray(int count)
        {
            if (count <= 0) return new List<string>();
            return Enumerable.Range(1, count).Select(i => $"D{i}").ToList();
        }

        // Get unassigned operators for station assignment
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

        // Get all stations for assignment (light DTO)
        public async Task<List<StationAssignmentDto>> GetAllStationsForAssignmentAsync()
        {
            // 1. Fetch ONLY Active stations using the new repository method
            var activeStations = await _stationRepository.GetActiveStationsAsync();

            // 2. Map to the light DTO for the UI
            var assignmentList = activeStations
                .Select(s => new StationAssignmentDto
                {
                    Id = s.Id,
                    StationName = s.StationName,
                    StationCode = s.StationCode
                })
                .ToList();

            return assignmentList;
        }



        // Update charging station details with partial update logic
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

            if (updateDto.StationOperatorIds.Any())
            {
                var operatorObjectIds = updateDto.StationOperatorIds
                    .Select(id => new ObjectId(id))
                    .ToList();

                updates.Add(updateBuilder.Set(s => s.StationOperatorIds, operatorObjectIds));
            }



            // Always update the UpdateAt timestamp
            updates.Add(updateBuilder.Set(s => s.UpdatedAt, DateTime.Now));

            // Final Check and Execution
            if (updates.Count == 0) return true; // Nothing to update

            var combinedUpdate = updateBuilder.Combine(updates);
            return await _stationRepository.PartialUpdateAsync(stationId, combinedUpdate);
        }

        // Get all stations with their upcoming bookings (max 2) and operator details       
        public async Task<List<StationWithBookingsDto>> GetStationsWithUpcomingBookingsAsync()
        {
            // IST/SLST Offset
            TimeSpan istOffset = TimeSpan.FromHours(5.5);

            // 1. Fetch ALL stations
            var allStations = await _stationRepository.GetAllStationsAsync();
            var stationObjectIds = allStations.Select(s => new ObjectId(s.Id)).ToList();

            // 2. Fetch ALL relevant upcoming bookings in ONE query
            var allUpcomingBookings = await _stationRepository.GetUpcomingBookingsByStationIdsAsync(stationObjectIds, 0);


            // 3. Collect all unique operator IDs from ALL stations
            var allOperatorIds = allStations
                .SelectMany(s => s.StationOperatorIds) // Flatten the List<ObjectId> from all stations
                .Distinct()
                .Select(oid => oid.ToString())
                .ToList();

            // 4. Fetch all operator user records in ONE batch query
            var allOperatorUsers = await _userRepository.FindManyByIdsAsync(allOperatorIds);
            var operatorLookup = allOperatorUsers.ToDictionary(u => u.Id, u => u);

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

                // --- JOIN OPERATOR DETAILS (One-to-Many) ---
                stationDto.AssignedOperators = station.StationOperatorIds
                    .Select(oid => oid.ToString())
                    .Where(id => operatorLookup.ContainsKey(id)) // Ensure user details were fetched
                    .Select(id => new SimpleOperatorDto
                    {
                        Id = id,
                        FullName = operatorLookup[id].FullName,
                        Email = operatorLookup[id].Email
                    })
                    .ToList();


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
        // Get all stations with operator details but WITHOUT bookings (for admin management view)
        public async Task<List<StationWithBookingsDto>> GetAllStationsWithDetailsAsync()
        {
            // 1. Fetch ALL stations
            var allStations = await _stationRepository.GetAllStationsAsync();

            // 2. Collect all unique operator IDs from ALL stations
            var allOperatorIds = allStations
                .SelectMany(s => s.StationOperatorIds) // Flatten the List<ObjectId> from all stations
                .Distinct()
                .Select(oid => oid.ToString())
                .ToList();

            // 3. Fetch all operator user records in ONE batch query
            var allOperatorUsers = await _userRepository.FindManyByIdsAsync(allOperatorIds);
            var operatorLookup = allOperatorUsers.ToDictionary(u => u.Id, u => u);

            // 4. Map Stations with operator details
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

                // --- JOIN OPERATOR DETAILS (One-to-Many) ---
                stationDto.AssignedOperators = station.StationOperatorIds
                    .Select(oid => oid.ToString())
                    .Where(id => operatorLookup.ContainsKey(id)) // Ensure user details were fetched
                    .Select(id => new SimpleOperatorDto
                    {
                        Id = id,
                        FullName = operatorLookup[id].FullName,
                        Email = operatorLookup[id].Email
                    })
                    .ToList();

                // Initialize empty upcoming bookings list for this endpoint
                stationDto.UpcomingBookings = new List<SimpleBookingDto>();

                return stationDto;
            }).ToList();

            return result;
        }



        public async Task<List<BookingDataDto>> GetStationManifestWithDetailsAsync(string stationId)
        {
            if (!ObjectId.TryParse(stationId, out var stationObjectId))
            {
                return new List<BookingDataDto>();
            }

            // 1. Fetch FILTERED bookings: ONLY for the specified station ID
            var stationBookings = await _bookingRepository.GetBookingsByStationIdAsync(stationObjectId);

            if (!stationBookings.Any()) return new List<BookingDataDto>();


            // --- STEP 2: BULK DATA RETRIEVAL (Joins) ---

            // Collect all necessary IDs for efficient batch lookups
            // NOTE: Using EVOwnerId (which is User._id stored in Booking) to join with EVOwnerProfile.UserId
            var userIds = stationBookings.Select(b => b.EVOwnerId).Distinct().ToList();
            var stationIds = stationBookings.Select(b => b.StationId).Distinct().ToList();

            // Fetch profiles and stations in parallel (efficiency!)
            // IMPORTANT: Using FindManyByUserIdsAsync to match Booking.EVOwnerId with EVOwnerProfile.UserId
            var profilesTask = _profileRepository.FindManyByUserIdsAsync(userIds);
            var stationsTask = _stationRepository.FindManyByIdsAsync(stationIds);

            await Task.WhenAll(profilesTask, stationsTask);

            // 3. Create fast lookup dictionaries
            // Key is UserId (matches Booking.EVOwnerId), NOT Profile._id
            var profileLookup = profilesTask.Result.ToDictionary(p => p.UserId, p => p);
            var stationLookup = stationsTask.Result.ToDictionary(s => new ObjectId(s.Id), s => s);

            // 4. Perform the in-memory 3-way join and mapping
            var result = stationBookings.Select(booking =>
            {
                // Join using Booking.EVOwnerId (which matches EVOwnerProfile.UserId)
                profileLookup.TryGetValue(booking.EVOwnerId, out var profile);
                stationLookup.TryGetValue(booking.StationId, out var station);

                // Map all fields explicitly to the clean DTO
                var dto = new BookingDataDto
                {
                    // --- Booking Data Mapping ---
                    Id = booking.Id,
                    EVOwnerId = booking.EVOwnerId.ToString(),
                    StationId = booking.StationId.ToString(),
                    SlotType = booking.SlotType,
                    SlotId = booking.SlotId,
                    StartTime = booking.StartTime,
                    EndTime = booking.EndTime,
                    Status = booking.Status,
                    QrCodeBase64 = booking.QrCodeBase64,
                    CreatedAt = booking.CreatedAt,
                    UpdatedAt = booking.UpdatedAt,
                    BookingDate = booking.BookingDate,

                    // --- Joined EV Owner Data ---
                    EVOwnerFullName = profile?.FullName ?? "N/A",
                    EVOwnerNIC = profile?.Nic ?? "N/A",

                    // --- Joined Station Data ---
                    StationName = station?.StationName ?? "N/A",
                    StationCode = station?.StationCode ?? "N/A"
                };

                return dto;
            }).ToList();

            return result;
        }


        // Reactivate a deactivated station
        public async Task<bool> ReactivateStationAsync(string stationId)
        {
            if (!ObjectId.TryParse(stationId, out var id)) return false;

            // Update station status to Active
            var updateDefinition = Builders<ChargingStation>.Update
                .Set(s => s.Status, "Active")
                .Set(s => s.UpdatedAt, DateTime.UtcNow);

            return await _stationRepository.PartialUpdateAsync(stationId, updateDefinition);
        }

    }
}