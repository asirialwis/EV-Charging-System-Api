//Data repository for dashboard related queries and aggregations
using MongoDB.Driver;
using EVChargingSystem.WebAPI.Data.Models;
using EVChargingSystem.WebAPI.Data;
using System;
using System.Threading.Tasks;
using System.Linq;
using EVChargingSystem.WebAPI.Data.Dtos;

namespace EVChargingSystem.WebAPI.Data.Repositories
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly IMongoCollection<Booking> _bookings;
        private readonly IMongoCollection<ChargingStation> _stations;

        public DashboardRepository(MongoDbContext context)
        {
            _bookings = context.GetCollection<Booking>("Bookings");
            _stations = context.GetCollection<ChargingStation>("ChargingStations");
        }

        // Count reservations by status, with option to filter for future bookings only
        public async Task<long> CountReservationsByStatusAsync(string status, bool futureOnly)
        {
            var filter = Builders<Booking>.Filter.Eq(b => b.Status, status);

            if (futureOnly)
            {
                // Only count bookings whose StartTime is in the future
                filter &= Builders<Booking>.Filter.Gt(b => b.StartTime, DateTime.UtcNow);
            }

            return await _bookings.CountDocumentsAsync(filter);
        }

        // Count stations by status
        public async Task<long> CountStationsByStatusAsync(string status)
        {
            var filter = Builders<ChargingStation>.Filter.Eq(s => s.Status, status);
            return await _stations.CountDocumentsAsync(filter);
        }

            // Count all stations
        public async Task<long> CountAllStationsAsync()
        {
            return await _stations.CountDocumentsAsync(Builders<ChargingStation>.Filter.Empty);
        }

        // Calculate today's capacity usage (used slots vs total slots) and return as a tuple
        public async Task<(long Used, long Total)> CalculateTodayCapacityAsync()
        {
            
            var activeStationFilter = Builders<ChargingStation>.Filter.Eq(s => s.Status, "Active");

          
            var totalCapacity = await _stations.Aggregate()
                .Match(activeStationFilter)
                .Group(
                    _ => 1, // Group all results into one document
                    g => new
                    {
                        TotalSlots = g.Sum(s => s.ACChargingSlots + s.DCChargingSlots)
                    }
                )
                .FirstOrDefaultAsync();

            long totalSlots = totalCapacity?.TotalSlots ?? 0;

            
            TimeSpan istOffset = TimeSpan.FromHours(5.5);

           
            var localToday = DateTime.UtcNow.Add(istOffset).Date;
            var utcDayStart = localToday.Subtract(istOffset);
            var utcDayEnd = localToday.AddDays(1).Subtract(istOffset);

           
            var usedSlotsFilter = Builders<Booking>.Filter.And(
                Builders<Booking>.Filter.Gte(b => b.StartTime, utcDayStart),
                Builders<Booking>.Filter.Lt(b => b.StartTime, utcDayEnd),
                Builders<Booking>.Filter.Or(
                    Builders<Booking>.Filter.Eq(b => b.Status, "Approved"),
                    Builders<Booking>.Filter.Eq(b => b.Status, "Pending")
                )
            );

            long usedSlots = await _bookings.CountDocumentsAsync(usedSlotsFilter);

          
            return (usedSlots, totalSlots);
        }
        
        // Get locations of all active charging stations
         public async Task<List<ChargingStationLocationDto>> GetActiveStationLocationsAsync()
        {
            var filter = Builders<ChargingStation>.Filter.Eq(s => s.Status, "Active");
            
            // --- Step 1: Execute the query and materialize the raw data ---
            // Fetch all active station documents into C# memory
            var rawStations = await _stations
                .Find(filter)
                .ToListAsync(); // <-- Query executed here
            
            // --- Step 2: Use LINQ-to-Objects to perform C#-specific conversion ---
            var locations = rawStations.Select(s =>
            {
                // We are now in C# memory, so TryParse and 'out var' are allowed.
                double.TryParse(s.Latitude, out var lat);
                double.TryParse(s.Longitude, out var lon);

                return new ChargingStationLocationDto
                {
                    StationId = s.Id.ToString(),
                    StationName = s.StationName,
                    StationCode = s.StationCode,
                    // Use the safely parsed values, defaulting to 0.0 if TryParse failed
                    Latitude = lat, 
                    Longitude = lon, 
                    Status = s.Status,
                    TotalCapacity = s.TotalCapacity.ToString()
                };
            }).ToList();

            return locations;
        }
        
    }
}