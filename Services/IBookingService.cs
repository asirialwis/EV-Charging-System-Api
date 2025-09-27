using EVChargingSystem.WebAPI.Data.Dtos;
using System.Data;
using System.Threading.Tasks;

namespace EVChargingSystem.WebAPI.Services
{
    // Define a DTO for the response payload
    public class SlotAvailabilityDto
    {
        public DateTime Date { get; set; }
        public List<string> AvailableSlots { get; set; } // e.g., ["14:00", "14:30", "15:00"]
    }
    public interface IBookingService
    {
        Task<bool> CreateBookingAsync(CreateBookingDto bookingDto);

        Task<List<string>> GetAvailableSlotsAsync(string stationId, string slotType, DateTime date);
    }
}