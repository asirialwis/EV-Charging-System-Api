using EVChargingSystem.WebAPI.Data.Models;
using System.Threading.Tasks;
using MongoDB.Bson;
using System;

namespace EVChargingSystem.WebAPI.Data.Repositories
{
    public interface IBookingRepository
    {
        Task<long> CountConflictingBookingsAsync(
            ObjectId stationId,
            string slotType,
            DateTime startTime,
            DateTime endTime);

        Task CreateAsync(Booking booking);


        Task<List<Booking>> GetApprovedBookingsForDayAsync(
            ObjectId stationId,
            string slotType,
            DateTime date);
    }
}