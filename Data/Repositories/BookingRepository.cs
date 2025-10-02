using MongoDB.Driver;
using EVChargingSystem.WebAPI.Data.Models;
using EVChargingSystem.WebAPI.Data;
using System;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace EVChargingSystem.WebAPI.Data.Repositories
{
    public class BookingRepository : IBookingRepository
    {
        private readonly IMongoCollection<Booking> _bookings;

        public BookingRepository(MongoDbContext context)
        {
            _bookings = context.GetCollection<Booking>("Bookings");
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

    }
}