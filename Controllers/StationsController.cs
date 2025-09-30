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
}