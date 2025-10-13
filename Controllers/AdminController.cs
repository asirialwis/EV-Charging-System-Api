//Admin handles booking approvals and dashboard metrics
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EVChargingSystem.WebAPI.Services;
using System.Threading.Tasks;
using EVChargingSystem.WebAPI.Data.Dtos;
using EVChargingApi.Services;

[ApiController]
[Route("api/[controller]")]
 // Only Backoffice and Operator can manage approvals
public class AdminController : ControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly IDashboardService _dashboardService;
    private readonly IUserService _userService;

    public AdminController(IBookingService bookingService, IDashboardService dashboardService, IUserService userService)
    {
        _bookingService = bookingService;
        _dashboardService = dashboardService;
        _userService = userService;
    }

    // approve booking for EV owner
    [HttpPost("approve-booking/{bookingId}")]
    [Authorize(Roles = "Backoffice,StationOperator")]
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

    //Fetch dashboard metrics
    [HttpGet("dashboard-metrics")]
    [Authorize(Roles = "Backoffice,StationOperator")]
    public async Task<IActionResult> GetDashboardMetrics()
    {
        var metrics = await _dashboardService.GetMetricsAsync();
        return Ok(metrics);
    }

    // Fetch active charging station locations
    [HttpGet("charging-stations/locations")]
    [Authorize(Roles = "Backoffice,StationOperator")]
    public async Task<IActionResult> GetStationLocations()
    {
        var locations = await _dashboardService.GetActiveStationLocationsAsync();
        return Ok(locations);
    }

    // Admin creates an EV Owner account
    [HttpPost("create-evowner")]
    [Authorize(Roles = "Backoffice,StationOperator")]
    public async Task<IActionResult> CreateEVOwnerByAdmin([FromBody] AdminCreateEVOwnerDto ownerDto)
    {
        // Admin must not be able to set the role or password
        // The DTO ensures the password is not provided, and the service sets the role.

        var (success, message) = await _userService.CreateOwnerByAdminAsync(ownerDto);

        if (!success)
        {
            return BadRequest(new { Message = message });
        }

        return Ok(new { Message = message });
    }

    // Admin fetches all operational users (Backoffice and Station Operators)
    [HttpGet("operational-users")]
    [Authorize(Roles = "Backoffice,StationOperator")]
    public async Task<IActionResult> GetAllOperationalUsers()
    {
        var users = await _userService.GetAllOperationalUsersAsync();
        return Ok(users);
    }




    [HttpGet("all-bookings")]
    [Authorize(Roles = "Backoffice,StationOperator")]
    public async Task<IActionResult> GetAllBookings()
    {
        var bookings = await _bookingService.GetAllBookingsWithDetailsAsync();
        return Ok(bookings);
    }

}