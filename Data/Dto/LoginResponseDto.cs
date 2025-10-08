using System.Collections.Generic;

namespace EVChargingSystem.WebAPI.Data.Dtos
{
    public class LoginResponseDto
    {
        // --- 1. Core Authentication & User Details (Always Present) ---
        
        public string Token { get; set; }
        public string Role { get; set; }
        public string Username { get; set; } // The user's email
        public string FullName { get; set; } 
        
        // --- 2. Station Operator Details (Nullable / Optional) ---
        
        // CRITICAL FIX: Use single nullable string for the ID and Name
        public string? AssignedStationId { get; set; } 
        public string? AssignedStationName { get; set; } 
    }
}