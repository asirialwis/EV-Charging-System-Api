using EVChargingSystem.WebAPI.Data.Dtos;
using EVChargingSystem.WebAPI.Data.Models;
using EVChargingSystem.WebAPI.Data.Repositories;
using MongoDB.Bson;
using System;
using System.Threading.Tasks;

namespace EVChargingSystem.WebAPI.Services
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IChargingStationRepository _stationRepository; // You need this for capacity check

        public BookingService(IBookingRepository bookingRepository, IChargingStationRepository stationRepository)
        {
            _bookingRepository = bookingRepository;
            _stationRepository = stationRepository;
        }

        public async Task<bool> CreateBookingAsync(CreateBookingDto bookingDto)
        {
            // Define the UTC offset for IST/SLST (+5:30)
            TimeSpan istOffset = TimeSpan.FromHours(5.5);

            // 1. Convert client's local StartTime/EndTime to UTC
            var utcStartTime = bookingDto.StartTime.Subtract(istOffset);
            var utcEndTime = bookingDto.EndTime.Subtract(istOffset);

            var stationId = new ObjectId(bookingDto.StationId);

            // ... (Capacity Check) ...

            // 2. Conflict Check: Count overlapping bookings (must use UTC times)
            var conflictingBookings = await _bookingRepository.CountConflictingBookingsAsync(
                stationId,
                bookingDto.SlotType,
                utcStartTime, // Pass UTC time to the repository
                utcEndTime
            );

            // ... (Check maxSlots vs conflictingBookings) ...

            // 3. Create the Booking
            var booking = new Booking
            {
                EVOwnerId = new ObjectId(bookingDto.EVOwnerId),
                StationId = stationId,
                SlotType = bookingDto.SlotType,
                StartTime = utcStartTime, // Store UTC time in DB
                EndTime = utcEndTime,     // Store UTC time in DB
                Status = "Pending",
                BookingDate = DateTime.UtcNow.Date,
                CreatedAt = DateTime.UtcNow
            };

            await _bookingRepository.CreateAsync(booking);
            return true;
        }




        public async Task<List<string>> GetAvailableSlotsAsync(string stationIdString, string slotType, DateTime date)
        {
            var availableSlots = new List<string>();
            var stationId = new ObjectId(stationIdString);
            var station = await _stationRepository.FindByIdAsync(stationId);

            // ... (Capacity and initial checks) ...

            // Define the UTC offset for IST/SLST (+5:30)
            TimeSpan istOffset = TimeSpan.FromHours(5.5);

            int maxSlots = (slotType == "AC") ? station.ACChargingSlots : station.DCChargingSlots;
            if (maxSlots <= 0) return availableSlots;
            var checkDate = date.Date;

            // *** MODIFIED LOGIC START ***
            var existingBookings = await _bookingRepository.GetApprovedBookingsForDayAsync(
                stationId,
                slotType,
                date.Date.Subtract(istOffset)); // Query DB using the UTC equivalent of the start of the day

            // Group existing bookings by their UTC start time
            var slotConflicts = existingBookings
                .GroupBy(b => b.StartTime)
                .ToDictionary(g => g.Key, g => g.Count());


            // Generate half-hour time slots based on the local time (what the user sees)
            for (int hour = 0; hour < 24; hour++)
            {
                for (int minute = 0; minute < 60; minute += 30)
                {
                    var slotStartLocal = checkDate.AddHours(hour).AddMinutes(minute);
                    var slotEndLocal = slotStartLocal.AddMinutes(30);

                    // 1. Get the UTC equivalent of the slot end time
                    var slotEndUtc = slotEndLocal.Subtract(istOffset);

                    // 2. Optimization: Skip past time slots (CHECK AGAINST UTC NOW)
                    if (slotEndUtc <= DateTime.UtcNow) continue;

                    // 3. Find the UTC Start Time to check against the slotConflicts dictionary
                    var slotStartUtc = slotStartLocal.Subtract(istOffset);

                    // 4. Check IN MEMORY using the UTC key
                    int conflictingCount = slotConflicts.GetValueOrDefault(slotStartUtc, 0);

                    // If conflicting bookings are less than max capacity, the slot is available
                    if (conflictingCount < maxSlots)
                    {
                        // Return the local time string (what the user expects)
                        availableSlots.Add(slotStartLocal.ToString("HH:mm"));
                    }
                }
            }
            // *** MODIFIED LOGIC END ***

            return availableSlots;
        }
    }
}