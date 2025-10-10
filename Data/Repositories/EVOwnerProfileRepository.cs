//EVowner profile management
using MongoDB.Driver;
using EVChargingApi.Data.Models;
using EVChargingSystem.WebAPI.Data;
using MongoDB.Bson;

namespace EVChargingApi.Data.Repositories
{
    public class EVOwnerProfileRepository : IEVOwnerProfileRepository
    {
        private readonly IMongoCollection<EVOwnerProfile> _profiles;

        public EVOwnerProfileRepository(MongoDbContext context)
        {
            _profiles = context.GetCollection<EVOwnerProfile>("EVOwnerProfiles");
        }

        //Find EVOwner by NIC
        public async Task<EVOwnerProfile> FindByNicAsync(string nic)
        {
            return await _profiles.Find(p => p.Nic == nic).FirstOrDefaultAsync();
        }

        //create ev owner
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

        //search evowner profile
        public async Task<EVOwnerProfile> FindByUserIdAsync(string userId)
        {

            if (!ObjectId.TryParse(userId, out var objectId))
            {

                return null;
            }


            var filter = Builders<EVOwnerProfile>.Filter.Eq(p => p.UserId, objectId);

            return await _profiles.Find(filter).FirstOrDefaultAsync();
        }

        //getallprofiles
        public async Task<List<EVOwnerProfile>> GetAllProfilesAsync()
        {
            return await _profiles.Find(_ => true).ToListAsync();
        }

        //delete evowner profile
        public async Task<bool> DeleteAsync(string nic)
        {
            // Define the filter to find the document by NIC
            var filter = Builders<EVOwnerProfile>.Filter.Eq(p => p.Nic, nic);
            
            // Execute the deletion
            var result = await _profiles.DeleteOneAsync(filter);
            
            // Return true if exactly one document was deleted
            return result.DeletedCount == 1;
        }
    }
}