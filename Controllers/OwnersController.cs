using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EVChargingSystem.WebAPI.Data.Dtos;
using EVChargingSystem.WebAPI.Services;
using System.Security.Claims; // To access the JWT claims
using System.Threading.Tasks;
using EVChargingApi.Services;

[ApiController]
[Route("api/evowners")] // Simplified route for both admin/owner access
// Allow both EVOwner and Backoffice roles access to this controller
[Authorize(Roles = "Backoffice, EVOwner")]
public class EVOwnersController : ControllerBase
{
    private readonly IUserService _userService;

    public EVOwnersController(IUserService userService)
    {
        _userService = userService;
    }


    // U - Update EV Owner Profile (Used by Admin and Owner)
    [HttpPatch("{nic}")]
    public async Task<IActionResult> UpdateEVOwner(string nic, [FromBody] UpdateEVOwnerDto updateDto)
    {
        // 1. Get Logged-in User ID and Role from JWT Claims
        var userId = User.FindFirst("id")?.Value;
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        if (userId == null || userRole == null) return Unauthorized("Invalid token credentials.");
        
        var success = await _userService.UpdateEVOwnerAsync(nic, updateDto, userId, userRole);
        
        if (!success) 
        {
            // Provides a generic error to prevent revealing if the NIC exists but access was denied
            return Unauthorized("Update failed. You are not authorized to modify this profile.");
        }
        return Ok(new { Message = "EV Owner profile updated." });
    }

}
    
    
    
   