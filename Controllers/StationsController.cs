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
}