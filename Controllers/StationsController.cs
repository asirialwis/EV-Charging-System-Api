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

    [HttpPost]
    [Authorize(Roles = "Backoffice")]
    public async Task<IActionResult> CreateChargingStation([FromBody] CreateStationDto stationDto)
    {
        await _stationService.CreateStationAsync(stationDto);
        return Ok(new { Message = "Charging station created successfully." });
    }

    [HttpGet("unassigned-operators")]
    [Authorize(Roles = "Backoffice")]
    public async Task<IActionResult> GetUnassignedOperators()
    {
        var operators = await _stationService.GetUnassignedOperatorsAsync();
        return Ok(operators);
    }

    [HttpGet("stations/all-for-assignment")]
    [Authorize(Roles = "Backoffice")]
    public async Task<IActionResult> GetAllStationsForAssignment()
    {
        var stations = await _stationService.GetAllStationsForAssignmentAsync();
        return Ok(stations);
    }



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


     [HttpGet("with-bookings")]
    [Authorize(Roles = "Backoffice")] // Assuming this is for Admin dashboard
    public async Task<IActionResult> GetStationsWithUpcomingBookings()
    {
        var stations = await _stationService.GetStationsWithUpcomingBookingsAsync();
        return Ok(stations);
    }

    [HttpGet("all")]
    [Authorize(Roles = "Backoffice")]
    public async Task<IActionResult> GetAllStationsWithDetails()
    {
        var stations = await _stationService.GetAllStationsWithDetailsAsync();
        return Ok(stations);
    }
}