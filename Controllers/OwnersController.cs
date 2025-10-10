using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EVChargingSystem.WebAPI.Data.Dtos;
using EVChargingSystem.WebAPI.Services;
using System.Security.Claims; // To access the JWT claims
using System.Threading.Tasks;
using EVChargingApi.Services;
using EVChargingApi.Data.Dto;

[ApiController]
[Route("api/evowners")] // Simplified route for both admin/owner access
// Allow both EVOwner and Backoffice roles access to this controller

public class EVOwnersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IEVOwnerService _evOwnerService;

    public EVOwnersController(IUserService userService, IEVOwnerService evOwnerService)
    {
        _userService = userService;
        _evOwnerService = evOwnerService;
    }


    // U - Update EV Owner Profile (Used by Admin and Owner)
    [HttpPatch("{nic}")]
    [Authorize(Roles = "Backoffice, EVOwner")]
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
            return Unauthorized("Update failed. You are not authorized to do this modification.");
        }
        return Ok(new { Message = "EV Owner profile updated." });
    }


    // Get EV Owner Profile (Used by Admin and Owner)
    [HttpGet("profile")]
    [Authorize(Roles = "EVOwner")]
    public async Task<IActionResult> GetOwnerProfile()
    {
        // 1. Extract the authenticated User ID from the JWT token
        var userId = User.FindFirst("id")?.Value;

        if (userId == null)
        {
            // Should not happen if Authorize worked, but is a safety net
            return Unauthorized("User ID claim missing.");
        }

        // 2. Call the service to retrieve the combined profile
        var profileDto = await _userService.GetOwnerProfileAsync(userId);

        if (profileDto == null)
        {
            return NotFound("EV Owner profile not found.");
        }

        return Ok(profileDto);
    }

    // Get all EV Owners (Admin and EVOwner themselves)
    [HttpGet]
    [Authorize(Roles = "Backoffice, EVOwner")]
    public async Task<IActionResult> GetAllEVOwners()
    {
        var owners = await _userService.GetAllEVOwnersAsync();
        return Ok(owners);
    }

    // Get EV Owner Details by NIC (Backoffice and Station Operator only)
    [HttpGet("details/{nic}")]
    [Authorize(Roles = "Backoffice,StationOperator")]
    public async Task<IActionResult> GetEVOwnerDetailsByNic(string nic)
    {
        try
        {
            var evOwnerDetails = await _evOwnerService.GetEVOwnerDetailsByNicAsync(nic);
            
            if (evOwnerDetails == null)
            {
                return NotFound(new { Message = "EV Owner not found with the provided NIC." });
            }

            return Ok(evOwnerDetails);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred while retrieving EV owner details.", Error = ex.Message });
        }
    }

    // Delete EV Owner (Admin only)
    [HttpDelete("profile/{nic}")] // Use a dedicated route segment for Admin deletes
    [Authorize(Roles = "Backoffice")] 
    public async Task<IActionResult> DeleteEVOwner(string nic)
    {
        var (success, message) = await _userService.DeleteEVOwnerAsync(nic);
        
        if (!success) 
        {
            return NotFound(new { Message = message });
        }
        
        return Ok(new { Message = message });
    }
}





