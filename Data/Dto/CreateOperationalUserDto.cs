namespace EVChargingSystem.WebAPI.Data.Dtos
{
    public class CreateOperationalUserDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }

        public string FullName { get; set; }


        public string Phone { get; set; }

        public List<string> AssignedStations { get; set; } = new List<string>(); // For Station Operators, link to their station
    }
}