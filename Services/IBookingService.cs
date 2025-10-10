using EVChargingSystem.WebAPI.Data.Dtos;
using EVChargingSystem.WebAPI.Data.Models;
using MongoDB.Bson;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace EVChargingSystem.WebAPI.Services
{
    // Define a DTO for the response payload
    public class SlotAvailabilityDto
    {
        public DateTime Date { get; set; }
        public List<string> AvailableSlots { get; set; } = new List<string>(); // e.g., ["14:00", "14:30", "15:00"]
    }

    public interface IBookingService
    {
        // Existing methods
        Task<(bool Success, string QrCodeBase64, string Message)> ApproveBookingAsync(string bookingId);
        Task<Booking> GetBookingDetails(ObjectId bookingId);
        Task<bool> FinalizeBookingAsync(ObjectId bookingId);

        // New methods for enhanced functionality
        Task<bool> CreateBookingByRoleAsync(CreateBookingDto dto, string userRole, string userId);
        Task<bool> UpdateBookingAsync(string bookingId, UpdateBookingDto dto, string userRole, string userId);
        Task<bool> CancelBookingAsync(string bookingId, string userId, string userRole);
        Task<bool> DeleteBookingAsync(string bookingId, string userRole);
        Task<BookingResponseDto?> GetBookingByIdAsync(string bookingId, string userId, string userRole);
        Task<List<BookingResponseDto>> GetBookingsForEVOwnerAsync(string evOwnerId);
        Task<List<BookingResponseDto>> GetBookingsForStationAsync(string stationId);
        Task<List<BookingResponseDto>> GetAllBookingsAsync();
        Task<AvailabilityResponseDto> GetAvailableSlotIdsAsync(AvailabilityRequestDto request, string? userRole);
        Task<OperatorBookingDetailDto?> GetFullBookingDetailsForOperatorAsync(ObjectId bookingId);
    }
}