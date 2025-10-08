using MongoDB.Driver;
using EVChargingSystem.WebAPI.Data.Models;
using EVChargingSystem.WebAPI.Data;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace EVChargingSystem.WebAPI.Data.Repositories
{
    public class UnwoundOperatorId
    {
        // This is the actual ObjectId value that was unwound from the array
        public ObjectId StationOperatorIds { get; set; }
    }

    public class ChargingStationRepository : IChargingStationRepository
    {
        private readonly IMongoCollection<ChargingStation> _stations;
        private readonly IMongoCollection<Booking> _bookings;

        public ChargingStationRepository(MongoDbContext context)
        {
            _stations = context.GetCollection<ChargingStation>("ChargingStations");
            _bookings = context.GetCollection<Booking>("Bookings");
        }

        public async Task CreateAsync(ChargingStation station)
        {
            await _stations.InsertOneAsync(station);
        }

        public async Task<ChargingStation> FindByIdAsync(ObjectId stationId)
        {
            // Implementation to find the station by its ObjectId
            return await _stations.Find(s => s.Id == stationId.ToString()).FirstOrDefaultAsync();
        }




        public async Task<List<string>> GetAllAssignedOperatorIdsAsync()
        {
            // The pipeline result is IMongoAggregateQueryable<string>, which can be used with ToListAsync()

            // --- FIX 2: Correct usage of var and pipeline definition ---
            var pipeline = _stations.Aggregate()
                // 1. Match/Filter (Ensure array is not null before unwinding)
                .Match(Builders<ChargingStation>.Filter.Ne(s => s.StationOperatorIds, null))

                // 2. Unwind: Deconstructs the array. The output type is the UnwoundOperatorId class defined above.
                .Unwind<ChargingStation, UnwoundOperatorId>(s => s.StationOperatorIds)

                // 3. Project: Now, use the strongly-typed property of the unwound class
                // Convert ObjectId to string here
                .Project(u => u.StationOperatorIds.ToString());


            // Execute the aggregation and collect the list of strings
            return await pipeline.ToListAsync();


        }

        public async Task<List<ChargingStation>> GetAllStationsAsync()
        {
            // Implementation to find ALL documents
            return await _stations.Find(_ => true).ToListAsync();
        }

        public async Task<List<ChargingStation>> GetActiveStationsAsync()
        {
            // Implementation to find ONLY documents where Status == "Active"
            var filter = Builders<ChargingStation>.Filter.Eq(s => s.Status, "Active");
            return await _stations.Find(filter).ToListAsync();
        }

        public async Task<bool> PartialUpdateAsync(string stationId, UpdateDefinition<ChargingStation> updateDefinition)
        {
            var filter = Builders<ChargingStation>.Filter.Eq(s => s.Id, stationId);

            // UpdateOneAsync performs the partial update
            var result = await _stations.UpdateOneAsync(filter, updateDefinition);

            return result.ModifiedCount == 1;
        }

        public async Task<List<Booking>> GetUpcomingBookingsByStationIdsAsync(List<ObjectId> stationIds, int limitPerStation)
        {
            var filter = Builders<Booking>.Filter.In(b => b.StationId, stationIds) &
                         // Status must be active/pending and StartTime must be in the future
                         Builders<Booking>.Filter.Gt(b => b.StartTime, DateTime.UtcNow) &
                         Builders<Booking>.Filter.Ne(b => b.Status, "Canceled") &
                         Builders<Booking>.Filter.Ne(b => b.Status, "Completed");

            // Fetch all future, relevant bookings, sorted by start time
            var pipeline = _bookings.Find(filter).SortBy(b => b.StartTime);

            return await pipeline.ToListAsync();
        }


        public async Task<bool> AddOperatorToStationsAsync(List<string> stationIds, string newOperatorId)
        {
            // 1. Convert NEW Operator ID to ObjectId 
            if (!ObjectId.TryParse(newOperatorId, out var operatorObjectId)) return false;

            // CRITICAL FIX: Ensure the list of station IDs is not empty before proceeding
            if (!stationIds.Any()) return false;

            // 2. Define the filter: Select all stations whose string ID is in the provided list

            var filter = Builders<ChargingStation>.Filter.In(s => s.Id, stationIds);

            // 3. Define the update: Atomically push the new Operator ID to the StationOperatorIds array
            var update = Builders<ChargingStation>.Update
                .Push(s => s.StationOperatorIds, operatorObjectId) // operatorObjectId is correctly BsonObjectId
                .Set(s => s.UpdatedAt, DateTime.UtcNow);

            // 4. Execute the update on multiple documents
            var result = await _stations.UpdateManyAsync(filter, update);

            // Check that at least one station was modified
            return result.ModifiedCount > 0;
        }


        public async Task<ChargingStation?> FindStationByOperatorIdAsync(string operatorId)
        {
            // CRITICAL STEP: Convert the string operatorId to an ObjectId
            if (!ObjectId.TryParse(operatorId, out var objectId)) return null;

            // Filter: Find the single station where the StationOperatorIds array CONTAINS the operator's ID.
            // The Many-to-One rule is enforced by the assumption that the operator's ID
            // will only appear once across the entire ChargingStation collection.
            var filter = Builders<ChargingStation>.Filter.AnyEq(s => s.StationOperatorIds, objectId);

            // Use FirstOrDefaultAsync() because the business rule enforces only one result.
            return await _stations.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<bool> AddOperatorToStationAsync(string assignedStationId, string id)
        {
            // Validate operator id
            if (!ObjectId.TryParse(id, out var operatorObjectId)) return false;

            // Filter by station string id
            var filter = Builders<ChargingStation>.Filter.Eq(s => s.Id, assignedStationId);

            // Push operator ObjectId into StationOperatorIds array and update timestamp
            var update = Builders<ChargingStation>.Update
                .Push(s => s.StationOperatorIds, operatorObjectId)
                .Set(s => s.UpdatedAt, DateTime.UtcNow);

            var result = await _stations.UpdateOneAsync(filter, update);

            return result.ModifiedCount == 1;
        }
    }
}