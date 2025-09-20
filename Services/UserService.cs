using MongoDB.Driver;
using EVChargingSystem.WebAPI.Data;

using EVChargingApi.Services;

namespace EVChargingSystem.WebAPI.Services
{
    public class UserService : IUserService
    {
        private readonly IMongoCollection<EVChargingApi.Data.Models.User> _users;

        public UserService(MongoDbContext context)
        {
            // the GetCollection method from the corrected MongoDbContext
            _users = context.GetCollection<EVChargingApi.Data.Models.User>("Users");
        }

        public async Task<EVChargingApi.Data.Models.User> AuthenticateAsync(string email, string password)
        {
            // use a secure password hashing algorithm
            var user = await _users.Find(u => u.Email == email && u.Password == password).FirstOrDefaultAsync();
            return user;
        }

        public async Task CreateAsync(EVChargingApi.Data.Models.User user)
        {
            // In a real app, hash the password before saving
            await _users.InsertOneAsync(user);
        }
    }
}