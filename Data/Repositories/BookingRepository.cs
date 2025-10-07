using MongoDB.Driver;
using EVChargingSystem.WebAPI.Data.Models;
using EVChargingSystem.WebAPI.Data;
using EVChargingSystem.WebAPI.Data.Dtos;
using EVChargingApi.Data.Models;
using System;
using System.Threading.Tasks;
using MongoDB.Bson;
using System.Collections.Generic;
using System.Linq;

namespace EVChargingSystem.WebAPI.Data.Repositories
{
    public class BookingRepository : IBookingRepository
    {
        private readonly IMongoCollection<Booking> _bookings;
        private readonly IMongoCollection<ChargingStation> _stations;
        private readonly IMongoCollection<EVOwnerProfile> _evOwnerProfiles;

        public BookingRepository(MongoDbContext context)
        {
            _bookings = context.GetCollection<Booking>("Bookings");
            _stations = context.GetCollection<ChargingStation>("ChargingStations");
            _evOwnerProfiles = context.GetCollection<EVOwnerProfile>("EVOwnerProfiles");
        }

        public async Task<long> CountConflictingBookingsAsync(
            ObjectId stationId,
            string slotType,
            DateTime startTime,
            DateTime endTime)
        {
            var filter = Builders<Booking>.Filter.And(
                // 1. Same Station and Slot Type
                Builders<Booking>.Filter.Eq(b => b.StationId, stationId),
                Builders<Booking>.Filter.Eq(b => b.SlotType, slotType),

                // 2. Status is not Canceled or Completed
                Builders<Booking>.Filter.Ne(b => b.Status, "Canceled"),
                Builders<Booking>.Filter.Ne(b => b.Status, "Completed"),

                // 3. Overlap Logic: (StartA < EndB) AND (EndA > StartB)
                Builders<Booking>.Filter.Lt(b => b.StartTime, endTime),
                Builders<Booking>.Filter.Gt(b => b.EndTime, startTime)
            );

            return await _bookings.CountDocumentsAsync(filter);
        }

        public async Task<List<Booking>> GetApprovedBookingsForDayAsync(
            ObjectId stationId,
            string slotType,
            DateTime date)
        {
            // Define the start and end of the entire day (00:00 to 23:59:59)
            var dayStart = date.Date;
            var dayEnd = date.Date.AddDays(1);

            var filter = Builders<Booking>.Filter.And(
                // 1. Same Station and Slot Type
                Builders<Booking>.Filter.Eq(b => b.StationId, stationId),
                Builders<Booking>.Filter.Eq(b => b.SlotType, slotType),

                // 2. Status is not Canceled or Completed
                Builders<Booking>.Filter.Ne(b => b.Status, "Canceled"),
                Builders<Booking>.Filter.Ne(b => b.Status, "Completed"),

                // 3. Booking must fall within the date being checked
                Builders<Booking>.Filter.Gte(b => b.StartTime, dayStart), // Start Time >= dayStart
                Builders<Booking>.Filter.Lt(b => b.StartTime, dayEnd)     // Start Time < dayEnd (before midnight)
            );

            // Execute ONE query to fetch ALL relevant bookings for the day
            return await _bookings.Find(filter).ToListAsync();
        }

        public async Task CreateAsync(Booking booking)
        {
            await _bookings.InsertOneAsync(booking);
        }


        public async Task<Booking> FindByIdAsync(ObjectId bookingId)
        {
            // Note: MongoDB driver often handles string conversion, but this uses ObjectId for the filter
            return await _bookings.Find(b => b.Id == bookingId.ToString()).FirstOrDefaultAsync();
        }

        public async Task<bool> UpdateStatusAsync(ObjectId bookingId, string newStatus)
        {
            var filter = Builders<Booking>.Filter.Eq(b => b.Id, bookingId.ToString());

            // Use the $set operator to update only the Status and the UpdatedAt timestamp
            var update = Builders<Booking>.Update
                .Set(b => b.Status, newStatus)
                .Set(b => b.UpdatedAt, DateTime.UtcNow);

            var result = await _bookings.UpdateOneAsync(filter, update);

            // Check if one document was matched and modified
            return result.IsAcknowledged && result.ModifiedCount == 1;
        }

        public async Task<bool> UpdateBookingAndQrCodeAsync(ObjectId bookingId, string newStatus, string qrCodeBase64)
        {
            var filter = Builders<Booking>.Filter.Eq(b => b.Id, bookingId.ToString());

            // Update the Status, the QrCode string, and the UpdatedAt timestamp
            var update = Builders<Booking>.Update
                .Set(b => b.Status, newStatus)
                .Set(b => b.QrCodeBase64, qrCodeBase64) // Requires QrCodeBase64 field in  Booking model
                .Set(b => b.UpdatedAt, DateTime.UtcNow);

            var result = await _bookings.UpdateOneAsync(filter, update);

            return result.IsAcknowledged && result.ModifiedCount == 1;
        }
        
         public async Task<bool> HasActiveBookingsForStationAsync(ObjectId stationId)
        {
            var filter = Builders<Booking>.Filter.And(
                // 1. Filter by Station ID
                Builders<Booking>.Filter.Eq(b => b.StationId, stationId),
                
                // 2. Filter by Status: Exclude 'Canceled' and 'Completed'
                Builders<Booking>.Filter.Ne(b => b.Status, "Canceled"),
                Builders<Booking>.Filter.Ne(b => b.Status, "Completed")
            );

            // Use CountDocumentsAsync and check if the count is greater than zero
            var count = await _bookings.CountDocumentsAsync(filter);

            return count > 0;
        }

        public async Task<(List<Booking> Bookings, long TotalCount)> GetBookingsByEVOwnerIdAsync(ObjectId evOwnerId, BookingFilterDto filter)
        {
            var baseFilter = Builders<Booking>.Filter.Eq(b => b.EVOwnerId, evOwnerId);
            
            // Apply status filter if provided
            if (!string.IsNullOrEmpty(filter.Status))
            {
                baseFilter = Builders<Booking>.Filter.And(baseFilter, Builders<Booking>.Filter.Eq(b => b.Status, filter.Status));
            }

            var totalCount = await _bookings.CountDocumentsAsync(baseFilter);
            
            var bookings = await _bookings
                .Find(baseFilter)
                .SortByDescending(b => b.CreatedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Limit(filter.PageSize)
                .ToListAsync();

            return (bookings, totalCount);
        }

        public async Task<(List<Booking> Bookings, long TotalCount)> GetBookingsByStationIdAsync(ObjectId stationId, BookingFilterDto filter)
        {
            var baseFilter = Builders<Booking>.Filter.Eq(b => b.StationId, stationId);
            
            // Apply status filter if provided
            if (!string.IsNullOrEmpty(filter.Status))
            {
                baseFilter = Builders<Booking>.Filter.And(baseFilter, Builders<Booking>.Filter.Eq(b => b.Status, filter.Status));
            }

            var totalCount = await _bookings.CountDocumentsAsync(baseFilter);
            
            var bookings = await _bookings
                .Find(baseFilter)
                .SortByDescending(b => b.CreatedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Limit(filter.PageSize)
                .ToListAsync();

            return (bookings, totalCount);
        }

        public async Task<(List<Booking> Bookings, long TotalCount)> GetAllBookingsAsync(BookingFilterDto filter)
        {
            var pipeline = new List<BsonDocument>();
            
            // Match stage for basic filters
            var matchStage = new BsonDocument("$match", new BsonDocument());
            
            // Apply status filter if provided
            if (!string.IsNullOrEmpty(filter.Status))
            {
                matchStage["$match"]["Status"] = filter.Status;
            }

            pipeline.Add(matchStage);

            // Lookup EVOwnerProfile for search functionality
            pipeline.Add(new BsonDocument("$lookup", new BsonDocument
            {
                { "from", "EVOwnerProfiles" },
                { "localField", "EVOwnerId" },
                { "foreignField", "UserId" },
                { "as", "evOwnerProfile" }
            }));

            // Lookup ChargingStation for search functionality
            pipeline.Add(new BsonDocument("$lookup", new BsonDocument
            {
                { "from", "ChargingStations" },
                { "localField", "StationId" },
                { "foreignField", "_id" },
                { "as", "station" }
            }));

            // Apply search filter if provided
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                var searchFilter = new BsonDocument("$or", new BsonArray
                {
                    new BsonDocument("evOwnerProfile.FullName", new BsonDocument("$regex", new BsonRegularExpression(filter.SearchTerm, "i"))),
                    new BsonDocument("evOwnerProfile.Nic", new BsonDocument("$regex", new BsonRegularExpression(filter.SearchTerm, "i"))),
                    new BsonDocument("station.StationName", new BsonDocument("$regex", new BsonRegularExpression(filter.SearchTerm, "i")))
                });
                pipeline.Add(new BsonDocument("$match", searchFilter));
            }

            // Count total documents
            var countPipeline = new List<BsonDocument>(pipeline);
            countPipeline.Add(new BsonDocument("$count", "total"));
            var countResult = await _bookings.Aggregate<BsonDocument>(countPipeline).FirstOrDefaultAsync();
            var totalCount = countResult?["total"]?.AsInt64 ?? 0;

            // Add sorting and pagination
            pipeline.Add(new BsonDocument("$sort", new BsonDocument("CreatedAt", -1)));
            pipeline.Add(new BsonDocument("$skip", (filter.PageNumber - 1) * filter.PageSize));
            pipeline.Add(new BsonDocument("$limit", filter.PageSize));

            var bookings = await _bookings.Aggregate<Booking>(pipeline).ToListAsync();

            return (bookings, totalCount);
        }

        public async Task<bool> UpdateBookingAsync(string bookingId, UpdateDefinition<Booking> update)
        {
            var filter = Builders<Booking>.Filter.Eq(b => b.Id, bookingId);
            var result = await _bookings.UpdateOneAsync(filter, update);
            return result.IsAcknowledged && result.ModifiedCount == 1;
        }

        public async Task<bool> DeleteBookingAsync(string bookingId)
        {
            var filter = Builders<Booking>.Filter.Eq(b => b.Id, bookingId);
            var result = await _bookings.DeleteOneAsync(filter);
            return result.IsAcknowledged && result.DeletedCount == 1;
        }

        public async Task<bool> CheckSlotAvailabilityAsync(ObjectId stationId, string slotId, DateTime start, DateTime end, string? excludeBookingId)
        {
            var filter = Builders<Booking>.Filter.And(
                Builders<Booking>.Filter.Eq(b => b.StationId, stationId),
                Builders<Booking>.Filter.Eq(b => b.SlotId, slotId),
                Builders<Booking>.Filter.Ne(b => b.Status, "Canceled"),
                Builders<Booking>.Filter.Ne(b => b.Status, "Completed"),
                Builders<Booking>.Filter.Lt(b => b.StartTime, end),
                Builders<Booking>.Filter.Gt(b => b.EndTime, start)
            );

            // Exclude current booking if updating
            if (!string.IsNullOrEmpty(excludeBookingId))
            {
                filter = Builders<Booking>.Filter.And(filter, Builders<Booking>.Filter.Ne(b => b.Id, excludeBookingId));
            }

            var count = await _bookings.CountDocumentsAsync(filter);
            return count == 0;
        }

        public async Task<List<string>> GetBookedSlotIdsAsync(ObjectId stationId, string slotType, DateTime start, DateTime end)
        {
            var filter = Builders<Booking>.Filter.And(
                Builders<Booking>.Filter.Eq(b => b.StationId, stationId),
                Builders<Booking>.Filter.Eq(b => b.SlotType, slotType),
                Builders<Booking>.Filter.Ne(b => b.Status, "Canceled"),
                Builders<Booking>.Filter.Ne(b => b.Status, "Completed"),
                Builders<Booking>.Filter.Lt(b => b.StartTime, end),
                Builders<Booking>.Filter.Gt(b => b.EndTime, start)
            );

            var bookings = await _bookings.Find(filter).ToListAsync();
            return bookings.Select(b => b.SlotId).Distinct().ToList();
        }

    }
}