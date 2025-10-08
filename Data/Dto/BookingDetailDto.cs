using System;

namespace EVChargingSystem.WebAPI.Data.Dtos
{
    public class OperatorBookingDetailDto
    {
        // Booking Details
        public string BookingId { get; set; }
        public string SlotType { get; set; }
        public string SlotId { get; set; }
        public DateTime StartTimeLocal { get; set; }
        public DateTime EndTimeLocal { get; set; }
        public string Status { get; set; }

        // EV Owner Details (from EVOwnerProfile)
        public string EVOwnerFullName { get; set; }
        public string NIC { get; set; }
        public string VehicleModel { get; set; }
        public string LicensePlate { get; set; }

        // Station Details (from ChargingStation)
        // public string StationId { get; set; }
        public string StationName { get; set; }
    }
}