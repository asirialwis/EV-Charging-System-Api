using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using EVChargingApi.Services;
using EVChargingApi.Dto;
using EVChargingApi.Data.Models;
using Microsoft.AspNetCore.Authorization;
using EVChargingSystem.WebAPI.Data.Dtos;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IConfiguration _config;

    public AuthController(IUserService userService, IConfiguration config)
    {
        _userService = userService;
        _config = config;
    }

    [HttpPost("register")]
    public async Task<IActionResult> RegisterEVOwner([FromBody] RegisterUserDto userDto)
    {
        if (userDto.Role != "EVOwner")
        {
            return BadRequest("Only EV owners can self-register.");
        }

        var success = await _userService.RegisterEVOwnerAsync(userDto);
        if (!success)
        {
            return BadRequest("Registration failed. Email or NIC may already be in use.");
        }

        return Ok(new { Message = "EV Owner registered successfully." });
    }

    
    [HttpPost("login")]
public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
{
    // Deconstruct the simplified Tuple result
    var (user, errorMessage, assignedStationId, stationName) = await _userService.AuthenticateAsync(request.Email, request.Password);

    // Check if the service returned an error
    if (user == null)
    {
        return Unauthorized(errorMessage);
    }

    // Authentication successful
    var token = GenerateJwtToken(user);
    
    // CRITICAL STEP: Construct the final response DTO
    var response = new LoginResponseDto
    {
        Token = token,
        Role = user.Role,
        Username = user.Email,
        FullName = user.FullName,
        
        // These fields are null unless specifically populated for the Station Operator role
        AssignedStationId = assignedStationId,
        AssignedStationName = stationName
    };

    return Ok(response);
}


     [HttpPost("create-operational-user")]
    [Authorize(Roles = "Backoffice")]
    public async Task<IActionResult> CreateOperationalUser([FromBody] CreateOperationalUserDto userDto)
    {
        if (userDto.Role != "Backoffice" && userDto.Role != "StationOperator")
        {
            return BadRequest("Invalid role. Role must be 'Backoffice' or 'StationOperator'.");
        }

        // Call the new transactional service method
        var (success, message) = await _userService.CreateOperatorAndAssignStationsAsync(userDto);
        
        if (!success)
        {
            return BadRequest(new { Message = message });
        }
        
        return Ok(new { Message = message });
    }

    private string GenerateJwtToken(User user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        // Use custom, simplified claim types
        var claims = new[]
        {
        new Claim("id", user.Id),
        new Claim("role", user.Role)
    };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddDays(3),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

