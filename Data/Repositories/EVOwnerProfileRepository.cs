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



        public async Task<List<EVOwnerProfile>> FindManyByUserIdsAsync(List<ObjectId> userIds)
        {
            // Filter: Find all profiles whose UserId is IN the provided list of ObjectIds
            var filter = Builders<EVOwnerProfile>.Filter.In(p => p.UserId, userIds);

            return await _profiles.Find(filter).ToListAsync();
        }


        public async Task<EVOwnerProfile> FindByIdAsync(ObjectId profileId)
        {
            // Filter: Find the profile where the _id matches the provided ObjectId
            var filter = Builders<EVOwnerProfile>.Filter.Eq(p => p.Id, profileId.ToString());

            return await _profiles.Find(filter).FirstOrDefaultAsync();
        }


        public async Task<List<EVOwnerProfile>> FindManyByProfileIdsAsync(List<ObjectId> profileIds)
        {
            // 1. Convert the list of ObjectId primary keys to string representation for the query filter
            var profileIdStrings = profileIds.Select(oid => oid.ToString()).ToList();

            // 2. Define the filter: Find all profiles whose _id (Id property) is IN the provided list of strings
            // The MongoDB C# driver efficiently handles the comparison of the string list against the BsonObjectId _id field.
            var filter = Builders<EVOwnerProfile>.Filter.In(p => p.Id, profileIdStrings);

            // 3. Execute the query
            return await _profiles.Find(filter).ToListAsync();
        }
    }
}