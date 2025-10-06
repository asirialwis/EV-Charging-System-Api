using System;

namespace EVChargingSystem.WebAPI.Data.Dtos
{
    public class BookingFilterDto
    {
        public string? SearchTerm { get; set; } // EV Owner name, NIC, or station name
        public string? Status { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
