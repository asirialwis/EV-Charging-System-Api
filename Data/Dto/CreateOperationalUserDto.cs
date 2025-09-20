namespace EVChargingSystem.WebAPI.Data.Dtos
{
    public class CreateOperationalUserDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
    }
}