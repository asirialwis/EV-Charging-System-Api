namespace EVChargingSystem.WebAPI.Data.Dtos
{
    public class DashboardMetricsDto
    {
        public int PendingReservations { get; set; }
        public int ApprovedFutureReservations { get; set; }
        public int ActiveStationsCount { get; set; }
        public int TotalStationsCount { get; set; }
        public double TodayCapacityPercentage { get; set; }
        public int TodayCapacityUsedSlots { get; set; }
        public int TodayCapacityTotalSlots { get; set; }
    }
}