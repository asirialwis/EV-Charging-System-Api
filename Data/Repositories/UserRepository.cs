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


         public async Task<bool> UpdateStatusAsync(string userId, string newStatus)
        {
            // 1. Define the filter: Find the User document by its string ID
            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            
            // 2. Define the update: Use $set to update only the Status and the UpdatedAt timestamp
            var update = Builders<User>.Update
                .Set(u => u.Status, newStatus)
                .Set(u => u.UpdatedAt, DateTime.UtcNow); // Assuming User model has UpdatedAt

            // 3. Execute the update
            var result = await _users.UpdateOneAsync(filter, update);
            
            // Return true if one document was matched and successfully modified
            return result.IsAcknowledged && result.ModifiedCount == 1;
        }

    }
}