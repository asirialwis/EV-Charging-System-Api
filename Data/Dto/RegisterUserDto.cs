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
    }
}