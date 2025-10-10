using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EVChargingApi.Dto
{
    public class RegisterUserDto
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
        public required string Role { get; set; }

        public string FullName { get; set; }
        public string Phone { get; set; }

        //profile fields for EVOwner
        public string Nic { get; set; }

        public string Address { get; set; }
        public string VehicleModel { get; set; }
        public string LicensePlate { get; set; }
    }
}