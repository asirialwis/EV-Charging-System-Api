using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EVChargingSystem.WebAPI.Data.Dtos;
using EVChargingSystem.WebAPI.Services;
using System.Security.Claims;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "EVOwner")] // Only EV Owners can create bookings
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingDto bookingDto)
    {
        // Get the EV Owner ID from the JWT token (Claim type "id")
        var evOwnerId = User.FindFirst("id")?.Value;
        if (evOwnerId == null) return Unauthorized("Invalid token: User ID not found.");

        bookingDto.EVOwnerId = evOwnerId; // Attach the authenticated user's ID

        var success = await _bookingService.CreateBookingAsync(bookingDto);

        if (!success)
        {
            return Conflict("Booking conflict: The requested time slot is full or invalid.");
        }

        return Ok(new { Message = "Booking successfully created and is pending approval." });
    }




    [HttpGet("available-slots")]
    [Authorize]
    public async Task<IActionResult> GetAvailableSlots(
    [FromQuery] string stationId,
    [FromQuery] string slotType,
    [FromQuery] DateTime date) // This correctly receives the parameter
    {
        if (string.IsNullOrWhiteSpace(stationId) || (slotType != "AC" && slotType != "DC") || date == DateTime.MinValue)
        {
            return BadRequest("Missing or invalid StationId, SlotType (AC/DC), or Date.");
        }

        // Enforcement of Assignment Requirement: Reservation date must be today or within the next 7 days
        if (date.Date > DateTime.UtcNow.Date.AddDays(7) || date.Date < DateTime.UtcNow.Date)
        {
            return BadRequest("Reservation date must be today or within the next 7 days.");
        }

        // Now, the service is called with the specific date received from the client.
        var availableSlots = await _bookingService.GetAvailableSlotsAsync(stationId, slotType, date.Date);

        return Ok(availableSlots);
    }
}