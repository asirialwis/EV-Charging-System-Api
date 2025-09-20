using MongoDB.Driver;
using EVChargingApi.Data.Models;

namespace EVChargingSystem.WebAPI.Data.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IMongoCollection<User> _users;

        public UserRepository(MongoDbContext context)
        {
            _users = context.GetCollection<User>("Users");
        }

        public async Task<User> FindByEmailAndPasswordAsync(string email, string password)
        {
            return await _users.Find(u => u.Email == email && u.Password == password).FirstOrDefaultAsync();
        }

        public async Task CreateAsync(User user)
        {
            await _users.InsertOneAsync(user);
        }

        public async Task<User> FindByEmailAsync(string email)
        {
            return await _users.Find(u => u.Email == email).FirstOrDefaultAsync();
        }

    }
}