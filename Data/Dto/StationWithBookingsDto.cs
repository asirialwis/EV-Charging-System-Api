using System.Collections.Generic;
using System;

namespace EVChargingSystem.WebAPI.Data.Dtos
{
    // Inner DTO for Booking Summary
    public class SimpleBookingDto
    {
        public string BookingId { get; set; }
        public DateTime StartTimeLocal { get; set; }
        public DateTime EndTimeLocal { get; set; }
        public string SlotType { get; set; }
    }
    
    public class SimpleOperatorDto
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
    }

    // Outer DTO for the Station and its bookings
    public class StationWithBookingsDto
    {
        public string Id { get; set; }
        public string StationName { get; set; }
        public string StationCode { get; set; }
        public int ACChargingSlots { get; set; }
        public int DCChargingSlots { get; set; }
        public List<string> ACSlots { get; set; } = new List<string>();
        public List<string> DCSlots { get; set; } = new List<string>();
        public string ACPowerOutput { get; set; }
        public string ACConnector { get; set; }
        public string ACChargingTime { get; set; }

        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public int TotalCapacity { get; set; }
        public string Status { get; set; }

        public string AdditionalNotes { get; set; }
          public List<SimpleOperatorDto> AssignedOperators { get; set; } = new List<SimpleOperatorDto>();
        public List<SimpleBookingDto> UpcomingBookings { get; set; } = new List<SimpleBookingDto>();
    }
}