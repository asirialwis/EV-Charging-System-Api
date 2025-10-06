using EVChargingSystem.WebAPI.Data.Models;
using EVChargingSystem.WebAPI.Data.Dtos;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;

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

        Task<Booking> FindByIdAsync(ObjectId bookingId);
        Task<bool> UpdateStatusAsync(ObjectId bookingId, string newStatus);
        Task<bool> UpdateBookingAndQrCodeAsync(ObjectId bookingId, string newStatus, string qrCodeBase64);
        Task<bool> HasActiveBookingsForStationAsync(ObjectId stationId);

        // New methods for enhanced functionality
        Task<(List<Booking> Bookings, long TotalCount)> GetBookingsByEVOwnerIdAsync(ObjectId evOwnerId, BookingFilterDto filter);
        Task<(List<Booking> Bookings, long TotalCount)> GetBookingsByStationIdAsync(ObjectId stationId, BookingFilterDto filter);
        Task<(List<Booking> Bookings, long TotalCount)> GetAllBookingsAsync(BookingFilterDto filter);
        Task<bool> UpdateBookingAsync(string bookingId, UpdateDefinition<Booking> update);
        Task<bool> DeleteBookingAsync(string bookingId);
        Task<bool> CheckSlotAvailabilityAsync(ObjectId stationId, string slotId, DateTime start, DateTime end, string? excludeBookingId);
        Task<List<string>> GetBookedSlotIdsAsync(ObjectId stationId, string slotType, DateTime start, DateTime end);
    }
}