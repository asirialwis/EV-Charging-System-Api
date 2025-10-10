// Repository for managing ChargingStation data
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
        // Create a new charging station
        public async Task CreateAsync(ChargingStation station)
        {
            await _stations.InsertOneAsync(station);
        }

        // Find a charging station by its ID
        public async Task<ChargingStation> FindByIdAsync(ObjectId stationId)
        {
            // Implementation to find the station by its ObjectId
            return await _stations.Find(s => s.Id == stationId.ToString()).FirstOrDefaultAsync();
        }


        // Get all unique operator IDs assigned to any station
        // Returns a list of strings (ObjectId as string)
        public async Task<List<string>> GetAllAssignedOperatorIdsAsync()
        {
            // The pipeline result is IMongoAggregateQueryable<string>, which can be used with ToListAsync()


            var pipeline = _stations.Aggregate()

                .Match(Builders<ChargingStation>.Filter.Ne(s => s.StationOperatorIds, null))


                .Unwind<ChargingStation, UnwoundOperatorId>(s => s.StationOperatorIds)


                .Project(u => u.StationOperatorIds.ToString());


            // Execute the aggregation and collect the list of strings
            return await pipeline.ToListAsync();


        }

        // Get all charging stations
        public async Task<List<ChargingStation>> GetAllStationsAsync()
        {
            // Implementation to find ALL documents
            return await _stations.Find(_ => true).ToListAsync();
        }

        // Get all active charging stations
        public async Task<List<ChargingStation>> GetActiveStationsAsync()
        {
            // Implementation to find ONLY documents where Status == "Active"
            var filter = Builders<ChargingStation>.Filter.Eq(s => s.Status, "Active");
            return await _stations.Find(filter).ToListAsync();
        }

        // Update a charging station by its ID
        public async Task<bool> PartialUpdateAsync(string stationId, UpdateDefinition<ChargingStation> updateDefinition)
        {
            var filter = Builders<ChargingStation>.Filter.Eq(s => s.Id, stationId);

            // UpdateOneAsync performs the partial update
            var result = await _stations.UpdateOneAsync(filter, updateDefinition);

            return result.ModifiedCount == 1;
        }

        // Get upcoming bookings for a list of station IDs, limited per station
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


        // Add a new operator to multiple stations atomically
        // Returns true if at least one station was updated
        public async Task<bool> AddOperatorToStationsAsync(List<string> stationIds, string newOperatorId)
        {
            // 1. Convert NEW Operator ID to ObjectId 
            if (!ObjectId.TryParse(newOperatorId, out var operatorObjectId)) return false;


            if (!stationIds.Any()) return false;



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

        // Find the single station assigned to a specific operator
        public async Task<ChargingStation?> FindStationByOperatorIdAsync(string operatorId)
        {

            if (!ObjectId.TryParse(operatorId, out var objectId)) return null;


            var filter = Builders<ChargingStation>.Filter.AnyEq(s => s.StationOperatorIds, objectId);


            return await _stations.Find(filter).FirstOrDefaultAsync();
        }

        // Add a new operator to a single station
        // Returns true if the station was updated
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