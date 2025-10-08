using EVChargingSystem.WebAPI.Data.Dtos;
using EVChargingSystem.WebAPI.Data.Models;
using EVChargingSystem.WebAPI.Data.Repositories;
using EVChargingSystem.WebAPI.Utils;
using EVChargingApi.Data.Models;
using EVChargingApi.Data.Repositories;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;


namespace EVChargingSystem.WebAPI.Services
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IChargingStationRepository _stationRepository;
        private readonly IEVOwnerProfileRepository _evOwnerProfileRepository;
        private readonly IUserRepository _userRepository;
        private readonly IEVOwnerProfileRepository _profileRepository;

        public BookingService(
            IBookingRepository bookingRepository,
            IChargingStationRepository stationRepository,
            IEVOwnerProfileRepository evOwnerProfileRepository,
            IUserRepository userRepository,
            IEVOwnerProfileRepository profileRepository)
        {
            _bookingRepository = bookingRepository;
            _stationRepository = stationRepository;
            _evOwnerProfileRepository = evOwnerProfileRepository;
            _userRepository = userRepository;
            _profileRepository = profileRepository;
        }

        /**
        * This method is used to get the available slot IDs for a given station, slot type, and time period
        * @param request - The request body containing the station id, slot type, and time details
        * @param userRole - The role of the user making the request
        * @returns The available slot IDs for the specified criteria
        **/
        public async Task<AvailabilityResponseDto> GetAvailableSlotIdsAsync(AvailabilityRequestDto request, string? userRole)
        {
            // 7-day validation
            var maxBookingDate = DateTime.UtcNow.AddDays(7);
            if (request.StartTime > maxBookingDate)
            {
                return new AvailabilityResponseDto
                {
                    IsAvailable = false,
                    Message = "Booking date must be within 7 days from today."
                };
            }

            // Convert to UTC
            TimeSpan istOffset = TimeSpan.FromHours(5.5);
            var utcStartTime = request.StartTime.Subtract(istOffset);
            var utcEndTime = request.EndTime.Subtract(istOffset);

            var stationId = new ObjectId(request.StationId);

            // Validate station exists and is active
            var station = await _stationRepository.FindByIdAsync(stationId);
            if (station == null || station.Status != "Active")
            {
                return new AvailabilityResponseDto
                {
                    IsAvailable = false,
                    Message = "Station not found or inactive."
                };
            }

            // Get station's slot array based on slot type
            var allSlots = request.SlotType == "AC" ? station.ACSlots : station.DCSlots;
            if (allSlots == null || !allSlots.Any())
            {
                return new AvailabilityResponseDto
                {
                    IsAvailable = false,
                    Message = $"No {request.SlotType} slots available at this station."
                };
            }

            // Get booked slot IDs for the time range
            var bookedSlotIds = await _bookingRepository.GetBookedSlotIdsAsync(
                stationId, request.SlotType, utcStartTime, utcEndTime);

            // Find available slots
            var availableSlotIds = allSlots.Except(bookedSlotIds).ToList();

            return new AvailabilityResponseDto
            {
                IsAvailable = availableSlotIds.Any(),
                AvailableSlotIds = availableSlotIds,
                Message = availableSlotIds.Any()
                    ? $"Found {availableSlotIds.Count} available {request.SlotType} slots."
                    : $"No available {request.SlotType} slots for the selected time period."
            };
        }

        /**
        * This method is used to create a booking for a given station, slot type, and time period
        * @param dto - The request body containing the EV Owner id, station id, slot type, slot id, and start and end time
        * @param userRole - The role of the user making the request
        * @param userId - The ID of the EV Owner which the booking is being created for
        * @returns True if the booking was created successfully, false otherwise
        **/
        public async Task<bool> CreateBookingByRoleAsync(CreateBookingDto dto, string userRole, string userId)
        {
            // 7-day validation
            var maxBookingDate = DateTime.UtcNow.AddDays(7);
            if (dto.StartTime > maxBookingDate)
            {
                return false; // Booking too far in future
            }

            // Convert to UTC
            TimeSpan istOffset = TimeSpan.FromHours(5.5);
            var utcStartTime = dto.StartTime.Subtract(istOffset);
            var utcEndTime = dto.EndTime.Subtract(istOffset);

            var stationId = new ObjectId(dto.StationId);

            // Validate station exists and is active
            var station = await _stationRepository.FindByIdAsync(stationId);
            if (station == null || station.Status != "Active")
            {
                return false;
            }

            // Check slot availability
            var isSlotAvailable = await _bookingRepository.CheckSlotAvailabilityAsync(
                stationId, dto.SlotId, utcStartTime, utcEndTime, null);

            if (!isSlotAvailable)
            {
                return false;
            }

            // Determine initial status based on role
            string initialStatus = userRole switch
            {
                "EVOwner" => "Pending",
                "Backoffice" or "StationOperator" => "Approved",
                _ => "Pending"
            };

            var booking = new Booking
            {
                EVOwnerId = new ObjectId(dto.EVOwnerId),
                StationId = stationId,
                SlotType = dto.SlotType,
                SlotId = dto.SlotId,
                StartTime = utcStartTime,
                EndTime = utcEndTime,
                Status = initialStatus,
                BookingDate = DateTime.UtcNow.Date,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _bookingRepository.CreateAsync(booking);

            // Generate QR code if approved
            if (initialStatus == "Approved")
            {
                var qrCode = QRCodeGeneratorUtil.GenerateQRCodeBase64(booking.Id);
                await _bookingRepository.UpdateBookingAndQrCodeAsync(
                    new ObjectId(booking.Id), "Approved", qrCode);
            }

            return true;
        }

        public async Task<(bool Success, string QrCodeBase64, string Message)> ApproveBookingAsync(string bookingId)
        {
            if (!ObjectId.TryParse(bookingId, out var objectId))
            {
                return (false, null, "Invalid Booking ID format.");
            }

            var booking = await _bookingRepository.FindByIdAsync(objectId);

            if (booking == null)
            {
                return (false, null, "Booking not found.");
            }
            if (booking.Status == "Approved" || booking.Status == "Completed")
            {
                return (false, null, "Booking is already approved or completed.");
            }

            // 1. Generate the QR Code (Payload is the Booking ID)
            string qrCodePayload = bookingId;
            string qrCodeBase64 = QRCodeGeneratorUtil.GenerateQRCodeBase64(qrCodePayload);

            // 2. Update status to 'Approved' and save the QR code to the document
            var success = await _bookingRepository.UpdateBookingAndQrCodeAsync(
                objectId,
                "Approved",
                qrCodeBase64
            );

            if (success)
            {
                return (true, qrCodeBase64, "Booking approved and QR code generated.");
            }
            else
            {
                return (false, null, "Failed to update booking status and QR code.");
            }
        }

        // Method for the Operator to look up details
        public async Task<Booking> GetBookingDetails(ObjectId bookingId)
        {
            return await _bookingRepository.FindByIdAsync(bookingId);
        }

        // Method for the Operator to finalize the session
        public async Task<bool> FinalizeBookingAsync(ObjectId bookingId)
        {
            // The operator finalizes the business once EV operation is done
            return await _bookingRepository.UpdateStatusAsync(bookingId, "Completed");
        }

        public async Task<bool> UpdateBookingAsync(string bookingId, UpdateBookingDto dto, string userRole, string userId)
        {
            if (!ObjectId.TryParse(bookingId, out var id)) return false;

            var booking = await _bookingRepository.FindByIdAsync(id);
            if (booking == null) return false;

            // 12-hour validation
            var timeUntilStart = booking.StartTime - DateTime.UtcNow;
            if (timeUntilStart.TotalHours < 12)
            {
                return false; // Too close to start time
            }

            // Convert times to UTC if provided
            TimeSpan istOffset = TimeSpan.FromHours(5.5);
            var utcStartTime = dto.StartTime?.Subtract(istOffset) ?? booking.StartTime;
            var utcEndTime = dto.EndTime?.Subtract(istOffset) ?? booking.EndTime;

            // Check slot availability if changing slot or time
            if (dto.SlotId != null || dto.StartTime != null || dto.EndTime != null)
            {
                var stationId = dto.StationId != null ? new ObjectId(dto.StationId) : booking.StationId;
                var slotId = dto.SlotId ?? booking.SlotId;

                var isSlotAvailable = await _bookingRepository.CheckSlotAvailabilityAsync(
                    stationId, slotId, utcStartTime, utcEndTime, bookingId);

                if (!isSlotAvailable)
                {
                    return false;
                }
            }

            // Determine new status based on role
            string newStatus = userRole switch
            {
                "EVOwner" => "Pending",
                "Backoffice" or "StationOperator" => "Approved",
                _ => "Pending"
            };

            // Build update definition
            var updateBuilder = Builders<Booking>.Update;
            var updates = new List<UpdateDefinition<Booking>>();

            if (dto.StationId != null) updates.Add(updateBuilder.Set(b => b.StationId, new ObjectId(dto.StationId)));
            if (dto.SlotType != null) updates.Add(updateBuilder.Set(b => b.SlotType, dto.SlotType));
            if (dto.SlotId != null) updates.Add(updateBuilder.Set(b => b.SlotId, dto.SlotId));
            if (dto.StartTime != null) updates.Add(updateBuilder.Set(b => b.StartTime, utcStartTime));
            if (dto.EndTime != null) updates.Add(updateBuilder.Set(b => b.EndTime, utcEndTime));

            updates.Add(updateBuilder.Set(b => b.Status, newStatus));
            updates.Add(updateBuilder.Set(b => b.UpdatedAt, DateTime.UtcNow));

            // Generate QR code if approved
            if (newStatus == "Approved")
            {
                var qrCode = QRCodeGeneratorUtil.GenerateQRCodeBase64(bookingId);
                updates.Add(updateBuilder.Set(b => b.QrCodeBase64, qrCode));
            }

            var combinedUpdate = updateBuilder.Combine(updates);
            return await _bookingRepository.UpdateBookingAsync(bookingId, combinedUpdate);
        }

        public async Task<bool> CancelBookingAsync(string bookingId, string userId, string userRole)
        {
            if (!ObjectId.TryParse(bookingId, out var id)) return false;

            var booking = await _bookingRepository.FindByIdAsync(id);
            if (booking == null) return false;

            // 12-hour validation
            var timeUntilStart = booking.StartTime - DateTime.UtcNow;
            if (timeUntilStart.TotalHours < 12)
            {
                return false; // Too close to start time
            }

            return await _bookingRepository.UpdateStatusAsync(id, "Canceled");
        }

        public async Task<bool> DeleteBookingAsync(string bookingId, string userRole)
        {
            if (userRole != "Backoffice" && userRole != "StationOperator") return false; // Only Backoffice and Operators can delete

            if (!ObjectId.TryParse(bookingId, out var id)) return false;

            var booking = await _bookingRepository.FindByIdAsync(id);
            if (booking == null) return false;

            // Only allow deletion of completed or canceled bookings
            if (booking.Status != "Completed" && booking.Status != "Canceled")
            {
                return false;
            }

            return await _bookingRepository.DeleteBookingAsync(bookingId);
        }

        public async Task<BookingResponseDto?> GetBookingByIdAsync(string bookingId, string userId, string userRole)
        {
            if (!ObjectId.TryParse(bookingId, out var id)) return null;

            var booking = await _bookingRepository.FindByIdAsync(id);
            if (booking == null) return null;

            // Authorization check
            if (userRole == "EVOwner" && booking.EVOwnerId.ToString() != userId)
            {
                return null; // EV Owner can only see their own bookings
            }

            if (userRole == "StationOperator")
            {
                // Check if operator is assigned to this station
                var user = await _userRepository.FindByIdAsync(userId);
                if (user == null || string.IsNullOrEmpty(user.AssignedStationId) || user.AssignedStationId != booking.StationId.ToString())
                {
                    return null; // Operator can only see bookings for their assigned stations
                }
            }

            return await MapToBookingResponseDto(booking);
        }

        public async Task<PagedResult<BookingResponseDto>> GetBookingsForEVOwnerAsync(string evOwnerId, BookingFilterDto filter)
        {
            var (bookings, totalCount) = await _bookingRepository.GetBookingsByEVOwnerIdAsync(
                new ObjectId(evOwnerId), filter);

            var responseDtos = new List<BookingResponseDto>();
            foreach (var booking in bookings)
            {
                var dto = await MapToBookingResponseDto(booking);
                if (dto != null) responseDtos.Add(dto);
            }

            return new PagedResult<BookingResponseDto>
            {
                Items = responseDtos,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };
        }

        public async Task<PagedResult<BookingResponseDto>> GetBookingsForStationAsync(string stationId, BookingFilterDto filter)
        {
            var (bookings, totalCount) = await _bookingRepository.GetBookingsByStationIdAsync(
                new ObjectId(stationId), filter);

            var responseDtos = new List<BookingResponseDto>();
            foreach (var booking in bookings)
            {
                var dto = await MapToBookingResponseDto(booking);
                if (dto != null) responseDtos.Add(dto);
            }

            return new PagedResult<BookingResponseDto>
            {
                Items = responseDtos,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };
        }

        public async Task<PagedResult<BookingResponseDto>> GetAllBookingsAsync(BookingFilterDto filter)
        {
            var (bookings, totalCount) = await _bookingRepository.GetAllBookingsAsync(filter);

            var responseDtos = new List<BookingResponseDto>();
            foreach (var booking in bookings)
            {
                var dto = await MapToBookingResponseDto(booking);
                if (dto != null) responseDtos.Add(dto);
            }

            return new PagedResult<BookingResponseDto>
            {
                Items = responseDtos,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };
        }



        private async Task<BookingResponseDto?> MapToBookingResponseDto(Booking booking)
        {
            try
            {
                // Get EV Owner profile
                var evOwnerProfile = await _evOwnerProfileRepository.FindByUserIdAsync(booking.EVOwnerId.ToString());

                // Get station details
                var station = await _stationRepository.FindByIdAsync(booking.StationId);

                if (evOwnerProfile == null || station == null)
                {
                    return null;
                }

                return new BookingResponseDto
                {
                    Id = booking.Id,
                    EVOwnerId = booking.EVOwnerId.ToString(),
                    EVOwnerName = evOwnerProfile.FullName,
                    EVOwnerNIC = evOwnerProfile.Nic,
                    StationId = booking.StationId.ToString(),
                    StationName = station.StationName,
                    StationCode = station.StationCode,
                    StartTime = booking.StartTime,
                    EndTime = booking.EndTime,
                    SlotType = booking.SlotType,
                    SlotId = booking.SlotId,
                    Status = booking.Status,
                    QrCodeBase64 = booking.QrCodeBase64,
                    BookingDate = booking.BookingDate,
                    CreatedAt = booking.CreatedAt,
                    UpdatedAt = booking.UpdatedAt
                };
            }
            catch
            {
                return null;
            }
        }
        




        public async Task<OperatorBookingDetailDto?> GetFullBookingDetailsForOperatorAsync(ObjectId bookingId)
{
    // IST/SLST Offset
    TimeSpan istOffset = TimeSpan.FromHours(5.5);

    // 1. Fetch the core Booking document
    var booking = await _bookingRepository.FindByIdAsync(bookingId);
    if (booking == null) return null;

    // 2. Fetch the EV Owner Profile (using the EVOwnerId from the booking)
    var profile = await _profileRepository.FindByUserIdAsync(booking.EVOwnerId.ToString());

    // 3. Fetch the Charging Station details (using the StationId from the booking)
    // You need a FindByIdAsync method in IChargingStationRepository for this.
    var station = await _stationRepository.FindByIdAsync(booking.StationId); 

    // If critical data (Profile or Station) is missing, fail gracefully
    if (profile == null || station == null) 
    {
        // Consider logging this as a data integrity error
        return null;
    }

    // 4. Map and return the combined DTO
    return new OperatorBookingDetailDto
    {
        BookingId = booking.Id,
        SlotType = booking.SlotType,
        SlotId = booking.SlotId,
        Status = booking.Status,
        
        // Convert UTC time to local IST/SLST time for the operator
        StartTimeLocal = booking.StartTime.Add(istOffset),
        EndTimeLocal = booking.EndTime.Add(istOffset),

        // Mapped Owner Details
        EVOwnerFullName = profile.FullName,
        NIC = profile.Nic,
        VehicleModel = profile.VehicleModel,
        LicensePlate = profile.LicensePlate,

        // Mapped Station Details
        // StationId = station.Id,
        StationName = station.StationName
    };
}
    }
}