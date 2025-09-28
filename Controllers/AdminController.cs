using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EVChargingSystem.WebAPI.Services;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Backoffice")] // Only Backoffice can manage approvals
public class AdminController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public AdminController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpPost("approve-booking/{bookingId}")]
    public async Task<IActionResult> ApproveBooking(string bookingId)
    {
        (bool success, string qrCode, string message) = await _bookingService.ApproveBookingAsync(bookingId);


        if (!success)
        {
            return BadRequest(new { Message = message });
        }

        // Return the QR code string so the Backoffice UI can store it or forward it
        return Ok(new { Message = message, QRCodeBase64 = qrCode });
    }
}