using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVChargingApi.Data.Models;

namespace EVChargingApi.Services
{
    public interface IUserService
{
    Task<User> AuthenticateAsync(string email, string password);
    Task CreateAsync(User user);
    
}
}