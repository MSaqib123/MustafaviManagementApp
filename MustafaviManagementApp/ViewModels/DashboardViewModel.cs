// ViewModels/DashboardViewModel.cs
using System.Collections.Generic;

namespace MustafaviManagementApp.ViewModels
{
    public class DashboardViewModel
    {
        public decimal TotalStock { get; set; }
        public decimal RemainingStock { get; set; }
        /* lists */
        public IList<StockLevelDto> LowStockItems { get; set; } = new List<StockLevelDto>();
        public IList<TimeSeriesPoint> DailySales { get; set; } = new List<TimeSeriesPoint>();
        public IList<TimeSeriesPoint> WeeklySales { get; set; } = new List<TimeSeriesPoint>();
        public IList<TimeSeriesPoint> DailyPurchases { get; set; } = new List<TimeSeriesPoint>();
        public IList<TimeSeriesPoint> WeeklyPurchases { get; set; } = new List<TimeSeriesPoint>();

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
