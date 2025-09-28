using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EVChargingSystem.WebAPI.Services;
using MongoDB.Bson;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "StationOperator")] // Only Station Operators can access these
public class OperatorController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public OperatorController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpGet("validate-qr/{bookingId}")]
    // EV Operator can read the QR code and confirm the data retrieved from the server
    public async Task<IActionResult> ValidateQR(string bookingId)
    {
        if (!ObjectId.TryParse(bookingId, out var id))
        {
            return BadRequest("Invalid QR code payload (Booking ID).");
        }

        var booking = await _bookingService.GetBookingDetails(id); 
        
        // Validation check
        if (booking == null || booking.Status != "Approved") // Only approved bookings can be validated
        {
            return NotFound("Booking not found or not yet approved.");
        }

        // Returns booking details for the operator to confirm
        return Ok(booking); 
    }

    [HttpPost("finalize/{bookingId}")]
    // Finalize the business once EV operation is done
    public async Task<IActionResult> FinalizeBooking(string bookingId)
    {
        if (!ObjectId.TryParse(bookingId, out var id))
        {
            return BadRequest("Invalid Booking ID.");
        }
        
        var success = await _bookingService.FinalizeBookingAsync(id);
        
        if (!success)
        {
            return BadRequest("Could not finalize booking. Check status or existence.");
        }

        return Ok(new { Message = "Charging session finalized and marked as completed." });
    }
}