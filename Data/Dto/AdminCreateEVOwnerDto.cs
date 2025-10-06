namespace EVChargingSystem.WebAPI.Data.Dtos
{
    public class AdminCreateEVOwnerDto
    {
        // Personal and Vehicle Details (same as mobile registration DTO)
        public string Email { get; set; }
        public string Nic { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string VehicleModel { get; set; }
        public string LicensePlate { get; set; }

        // Role is implicitly EVOwner: we will set it in the service
    }
}