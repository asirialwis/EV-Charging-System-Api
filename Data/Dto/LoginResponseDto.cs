using System.Collections.Generic;

namespace EVChargingSystem.WebAPI.Data.Dtos
{
    public class LoginResponseDto
    {
        // --- 1. Core Authentication & User Details (Always Present) ---
        
        public string Token { get; set; }
        public string Role { get; set; }
        public string FullName { get; set; } 
        
        public string? AssignedStationId { get; set; } 
        public string? AssignedStationName { get; set; } 
    }
}