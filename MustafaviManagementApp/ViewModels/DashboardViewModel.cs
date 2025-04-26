// ViewModels/DashboardViewModel.cs
using System.Collections.Generic;

namespace MustafaviManagementApp.ViewModels
{
    public class DashboardViewModel
    {
        public decimal TotalStock { get; set; }
        public decimal RemainingStock { get; set; }
        public List<StockLevelDto> LowStockItems { get; set; }

        public List<TimeSeriesPoint> DailySales { get; set; }
        public List<TimeSeriesPoint> WeeklySales { get; set; }
        public List<TimeSeriesPoint> DailyPurchases { get; set; }
        public List<TimeSeriesPoint> WeeklyPurchases { get; set; }

        public decimal TotalInventoryValue { get; set; }
        public decimal RemainingInventoryValue { get; set; }
        public decimal Revenue { get; set; }
    }

    public class StockLevelDto
    {
        public string MedicineName { get; set; }
        public int Quantity { get; set; }
    }

    public class TimeSeriesPoint
    {
        public string Period { get; set; }
        public decimal Value { get; set; }
    }
}
