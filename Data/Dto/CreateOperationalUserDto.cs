namespace EVChargingSystem.WebAPI.Data.Dtos
{
    public class CreateOperationalUserDto
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
        public required string Role { get; set; }
        public required string FullName { get; set; }
        public string? Phone { get; set; }
        public string? AssignedStationId { get; set; }
    }
}