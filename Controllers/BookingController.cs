// This controller manages booking-related operations including creation, approval, updating, and cancellation of bookings.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EVChargingSystem.WebAPI.Data.Dtos;
using EVChargingSystem.WebAPI.Services;
using System.Security.Claims;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class BookingController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    // Helper methods to extract user info from JWT claims
    private string GetUserId()
    {
        return User.FindFirst("id")?.Value ?? throw new UnauthorizedAccessException("User ID not found in token.");
    }

    // Extract user role from JWT claims
    private string GetUserRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value ?? throw new UnauthorizedAccessException("User role not found in token.");
    }

    // Check availability of charging slots
    [HttpPost("check-availability")]
    [Authorize(Roles = "EVOwner,Backoffice,StationOperator")]
    public async Task<IActionResult> CheckAvailability([FromBody] AvailabilityRequestDto request)
    {
        try
        {
            var userId = GetUserId();
            var userRole = GetUserRole();
            var result = await _bookingService.GetAvailableSlotIdsAsync(request, userRole);

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }
    // Create a new booking
    [HttpPost]
    [Authorize(Roles = "EVOwner,Backoffice,StationOperator")]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingDto bookingDto)
    {
        try
        {
            var userId = GetUserId();
            var userRole = GetUserRole();
            
            if (userRole == "EVOwner")
            {
                bookingDto.EVOwnerId = userId;
            }
            else if (string.IsNullOrEmpty(bookingDto.EVOwnerId))
            {
                return BadRequest("EVOwnerId is required when creating booking for another user.");
            }

            var success = await _bookingService.CreateBookingByRoleAsync(bookingDto, userRole, userId);

            if (!success)
            {
                return BadRequest("Failed to create booking. Please check station availability and booking constraints.");
            }

            var message = userRole == "EVOwner" 
                ? "Booking successfully created and is pending approval." 
                : "Booking successfully created and approved.";

            return Ok(new { Message = message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    // Get booking by ID with role-based access
    [HttpGet("{id}")]
    [Authorize(Roles = "EVOwner,Backoffice,StationOperator")]
    public async Task<IActionResult> GetBookingById(string id)
    {
        try
        {
            var userId = GetUserId();
            var userRole = GetUserRole();

            var booking = await _bookingService.GetBookingByIdAsync(id, userId, userRole);

            if (booking == null)
            {
                return NotFound("Booking not found or you don't have permission to view it.");
            }

            return Ok(booking);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }
    // Get bookings for the authenticated EV Owner
    [HttpGet("my-bookings")]
    [Authorize(Roles = "EVOwner")]
    public async Task<IActionResult> GetMyBookings([FromQuery] BookingFilterDto filter)
    {
        try
        {
            var evOwnerId = GetUserId();

            var result = await _bookingService.GetBookingsForEVOwnerAsync(evOwnerId, filter);

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    // Get bookings for a specific station (Station Operator only)
    [HttpGet("station/{stationId}")]
    [Authorize(Roles = "StationOperator")]
    public async Task<IActionResult> GetBookingsByStation(string stationId, [FromQuery] BookingFilterDto filter)
    {
        try
        {
            var result = await _bookingService.GetBookingsForStationAsync(stationId, filter);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    // Get all bookings (Backoffice only) with optional filtering
    [HttpGet("all")]
    [Authorize(Roles = "Backoffice")]
    public async Task<IActionResult> GetAllBookings([FromQuery] BookingFilterDto filter)
    {
        try
        {
            var result = await _bookingService.GetAllBookingsAsync(filter);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

        // Update an existing booking
    [HttpPut("{id}")]
    [Authorize(Roles = "EVOwner,Backoffice,StationOperator")]
    public async Task<IActionResult> UpdateBooking(string id, [FromBody] UpdateBookingDto dto)
    {
        try
        {
            var userId = GetUserId();
            var userRole = GetUserRole();

            var success = await _bookingService.UpdateBookingAsync(id, dto, userRole, userId);

            if (!success)
            {
                return BadRequest("Failed to update booking. Please check constraints and permissions.");
            }

            var message = userRole == "EVOwner" 
                ? "Booking updated successfully and is pending approval." 
                : "Booking updated successfully and approved.";

            return Ok(new { Message = message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

// Approve a booking (Backoffice and Station Operator)
    [HttpPost("{id}/approve")]
    [Authorize(Roles = "Backoffice,StationOperator")] 
    public async Task<IActionResult> ApproveBooking(string id)
    {
        try
        {
            (bool success, string qrCode, string message) = await _bookingService.ApproveBookingAsync(id);

            if (!success)
            {
                return BadRequest(new { Message = message });
            }

            return Ok(new { Message = message, QRCodeBase64 = qrCode });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    // Cancel a booking (EVOwner can cancel their own, Backoffice and Station Operator can cancel any)
    [HttpDelete("{id}")]
    [Authorize(Roles = "EVOwner,Backoffice,StationOperator")]
    public async Task<IActionResult> CancelBooking(string id)
    {
        try
        {
            var userId = GetUserId();
            var userRole = GetUserRole();

            var success = await _bookingService.CancelBookingAsync(id, userId, userRole);

            if (!success)
            {
                return BadRequest("Failed to cancel booking. Please check constraints and permissions.");
            }

            return Ok(new { Message = "Booking cancelled successfully." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

// Permanently delete a booking (Backoffice and Station Operator only)
    [HttpDelete("{id}/permanent")]
    [Authorize(Roles = "Backoffice,StationOperator")]
    public async Task<IActionResult> DeleteBooking(string id)
    {
        try
        {
            var userRole = GetUserRole();

            var success = await _bookingService.DeleteBookingAsync(id, userRole);


            if (!success)
            {
                return BadRequest("Failed to delete booking. Only completed or cancelled bookings can be deleted.");
            }

            return Ok(new { Message = "Booking deleted successfully." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }
}