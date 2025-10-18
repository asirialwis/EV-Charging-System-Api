//stations controller
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EVChargingSystem.WebAPI.Data.Dtos;
using EVChargingSystem.WebAPI.Services;
using System.Threading.Tasks;


[ApiController]
[Route("api/[controller]")]
public class StationsController : ControllerBase
{
    private readonly IChargingStationService _stationService;

    public StationsController(IChargingStationService stationService)
    {
        _stationService = stationService;
    }

    // Create a new charging station (Admin only)
    [HttpPost]
    [Authorize(Roles = "Backoffice")]
    public async Task<IActionResult> CreateChargingStation([FromBody] CreateStationDto stationDto)
    {
        await _stationService.CreateStationAsync(stationDto);
        return Ok(new { Message = "Charging station created successfully." });
    }
    // Get unassigned operators for station assignment (Admin only)
    [HttpGet("unassigned-operators")]
    [Authorize(Roles = "Backoffice")]
    public async Task<IActionResult> GetUnassignedOperators()
    {
        var operators = await _stationService.GetUnassignedOperatorsAsync();
        return Ok(operators);
    }
    // Get all stations for assignment (Admin only)
    [HttpGet("stations/all-for-assignment")]
    [Authorize(Roles = "Backoffice")]
    public async Task<IActionResult> GetAllStationsForAssignment()
    {
        var stations = await _stationService.GetAllStationsForAssignmentAsync();
        return Ok(stations);
    }


    // Update charging station details
    [HttpPatch("{id}")] // Use PATCH for partial updates
    [Authorize(Roles = "Backoffice")] // Only Backoffice can manage station details
    public async Task<IActionResult> UpdateChargingStation(string id, [FromBody] UpdateStationDto updateDto)
    {
        var success = await _stationService.UpdateStationAsync(id, updateDto);

        if (!success)
        {
            // If the update fails, it could be due to invalid ID, or the active booking restriction
            return BadRequest("Update failed. Station ID is invalid or active bookings exist (cannot deactivate).");
        }

        return Ok(new { Message = "Charging station details updated successfully." });
    }

    // Get stations with upcoming bookings (for Admin dashboard)
     [HttpGet("with-bookings")]
    [Authorize(Roles = "Backoffice")] // Assuming this is for Admin dashboard
    public async Task<IActionResult> GetStationsWithUpcomingBookings()
    {
        var stations = await _stationService.GetStationsWithUpcomingBookingsAsync();
        return Ok(stations);
    }

    // Get all stations with details (Admin only)
    [HttpGet("all")]
    [Authorize(Roles = "Backoffice")]
    public async Task<IActionResult> GetAllStationsWithDetails()
    {
        var stations = await _stationService.GetAllStationsWithDetailsAsync();
        return Ok(stations);
    }

    // Reactivate a deactivated station (Admin only)
    [HttpPost("{id}/reactivate")]
    [Authorize(Roles = "Backoffice")]
    public async Task<IActionResult> ReactivateStation(string id)
    {
        var success = await _stationService.ReactivateStationAsync(id);

        if (!success)
        {
            return BadRequest("Failed to reactivate station. Station ID may be invalid.");
        }

        return Ok(new { Message = "Station reactivated successfully." });
    }
}