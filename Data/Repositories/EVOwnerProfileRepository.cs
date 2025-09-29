using MongoDB.Driver;
using EVChargingApi.Data.Models;
using EVChargingSystem.WebAPI.Data;

namespace EVChargingApi.Data.Repositories
{
    public class EVOwnerProfileRepository : IEVOwnerProfileRepository
    {
        private readonly IMongoCollection<EVOwnerProfile> _profiles;

        public EVOwnerProfileRepository(MongoDbContext context)
        {
            _profiles = context.GetCollection<EVOwnerProfile>("EVOwnerProfiles");
        }

        public async Task<EVOwnerProfile> FindByNicAsync(string nic)
        {
            return await _profiles.Find(p => p.Nic == nic).FirstOrDefaultAsync();
        }

        public async Task CreateAsync(EVOwnerProfile profile)
        {
            await _profiles.InsertOneAsync(profile);
        }
        public async Task<bool> PartialUpdateAsync(string nic, UpdateDefinition<EVOwnerProfile> updateDefinition)
        {
            var filter = Builders<EVOwnerProfile>.Filter.Eq(p => p.Nic, nic);

            // UpdateOneAsync performs the partial update
            var result = await _profiles.UpdateOneAsync(filter, updateDefinition);

            return result.ModifiedCount == 1;
        }
    }
}