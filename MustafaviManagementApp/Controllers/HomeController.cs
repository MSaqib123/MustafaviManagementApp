using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using MedicineStore.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MustafaviManagementApp.Models;
using MustafaviManagementApp.ViewModels;

namespace MustafaviManagementApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _db;
        public HomeController(ILogger<HomeController> logger, AppDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var cal = CultureInfo.CurrentCulture.Calendar;

            /* ───────────────────────────────────────────────
               1) Stock numbers – pure StockLedger
               ─────────────────────────────────────────────── */

            // 1-a  total units ever IN (positive qtyChange only)
            var totalStockEver = await _db.StockLedgers
                                   .Where(l => l.QtyChange > 0)
                                   .SumAsync(l => (int?)l.QtyChange) ?? 0;

            // 1-b  latest balance per medicine → on-hand
            var latestBalances = await _db.StockLedgers
                                    .GroupBy(l => l.MedicineId)
                                    .Select(g => g.OrderByDescending(l => l.StockLedgerId)
                                                  .FirstOrDefault())
                                    .ToListAsync();
            var remainingStock = latestBalances.Sum(l => l?.BalanceAfter ?? 0);

            // 1-c  low-stock (<5) list
            var lowStockItems = latestBalances
                .Where(l => l != null && l.BalanceAfter < 5)
                .Join(_db.Medicines,
                      ledg => ledg.MedicineId,
                      med => med.MedicineId,
                      (ledg, med) => new StockLevelDto
                      {
                          MedicineName = med.MedicineName,
                          Quantity = ledg.BalanceAfter
                      })
                .OrderBy(x => x.Quantity)
                .ToList();

            /* ───────────────────────────────────────────────
               2) Sales & Purchases (unchanged)
               ─────────────────────────────────────────────── */
            /* ----------- time-series helpers ----------- */
            TimeSeriesPoint MapSeries(DateTime d, decimal? total) =>
                new() { Period = d.ToString("yyyy-MM-dd"), Value = total ?? 0m };

            /* 2-a  Daily Sales last 7d */
            var rawDailySales = await _db.Sales
                .Where(s => !s.IsHeld && s.SaleDate >= today.AddDays(-6))
                .GroupBy(s => s.SaleDate.Date)
                .Select(g => new { Date = g.Key, Total = g.Sum(s => s.TotalAmount) })
                .ToListAsync();

            var dailySales = Enumerable.Range(0, 7)
                .Select(i => {
                    var d = today.AddDays(-6 + i);
                    var rec = rawDailySales.FirstOrDefault(x => x.Date == d);
                    return MapSeries(d, rec?.Total);
                }).ToList();

            /* 2-b  Weekly Sales last 4w */
            var recentSales = await _db.Sales
                .Where(s => !s.IsHeld && s.SaleDate >= today.AddDays(-27))
                .Select(s => new { s.SaleDate, s.TotalAmount })
                .ToListAsync();

            var rawWeeklySales = recentSales
                .GroupBy(x => cal.GetWeekOfYear(x.SaleDate,
                         CalendarWeekRule.FirstDay, DayOfWeek.Monday))
                .Select(g => new { Week = g.Key, Total = g.Sum(x => x.TotalAmount) })
                .ToList();

            var curWk = cal.GetWeekOfYear(today,
                          CalendarWeekRule.FirstDay, DayOfWeek.Monday);
            var weeklySales = Enumerable.Range(0, 4).Select(i => {
                var wk = curWk - 3 + i;
                var rec = rawWeeklySales.FirstOrDefault(x => x.Week == wk);
                return new TimeSeriesPoint { Period = "W" + wk, Value = rec?.Total ?? 0m };
            }).ToList();

            /* 2-c  Daily Purchases last 7d */
            var rawDailyPurch = await _db.Purchases
                .Where(p => p.PurchaseDate >= today.AddDays(-6))
                .GroupBy(p => p.PurchaseDate.Date)
                .Select(g => new { Date = g.Key, Total = g.Sum(p => p.TotalCost) })
                .ToListAsync();

            var dailyPurch = Enumerable.Range(0, 7).Select(i => {
                var d = today.AddDays(-6 + i);
                var rec = rawDailyPurch.FirstOrDefault(x => x.Date == d);
                return MapSeries(d, rec?.Total);
            }).ToList();

            /* 2-d  Weekly Purchases last 4w */
            var recentPurch = await _db.Purchases
                .Where(p => p.PurchaseDate >= today.AddDays(-27))
                .Select(p => new { p.PurchaseDate, p.TotalCost })
                .ToListAsync();

            var rawWeeklyPurch = recentPurch
                .GroupBy(x => cal.GetWeekOfYear(x.PurchaseDate,
                         CalendarWeekRule.FirstDay, DayOfWeek.Monday))
                .Select(g => new { Week = g.Key, Total = g.Sum(x => x.TotalCost) })
                .ToList();

            var weeklyPurch = Enumerable.Range(0, 4).Select(i => {
                var wk = curWk - 3 + i;
                var rec = rawWeeklyPurch.FirstOrDefault(x => x.Week == wk);
                return new TimeSeriesPoint { Period = "W" + wk, Value = rec?.Total ?? 0m };
            }).ToList();

            /* ───────────────────────────────────────────── */
            /* 3) Inventory value + revenue (unchanged)    */
            /* ───────────────────────────────────────────── */
            var totalInvValue = await _db.PurchaseDetails
                .SumAsync(pd => pd.Quantity * pd.CostPrice);

            var remainingInvValue = remainingStock == 0
                 ? 0m
                 : latestBalances
                     .Join(_db.Medicines, l => l.MedicineId, m => m.MedicineId,
                           (l, m) => (decimal)l.BalanceAfter *
                                   (m.SingleUnitPrice ?? m.CotanUnitPrice ?? 0m))
                     .Sum();

            var revenue = await _db.Sales
                .Where(s => !s.IsHeld)
                .SumAsync(s => s.TotalAmount);

            /* 4) Build ViewModel */
            var vm = new DashboardViewModel
            {
                TotalStock = totalStockEver,
                RemainingStock = remainingStock,
                LowStockItems = lowStockItems,
                DailySales = dailySales,
                WeeklySales = weeklySales,
                DailyPurchases = dailyPurch,
                WeeklyPurchases = weeklyPurch,
                TotalInventoryValue = totalInvValue,
                RemainingInventoryValue = remainingInvValue,
                Revenue = revenue
            };
            return View(vm);
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() =>
            View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}


//using System;
//using System.Diagnostics;
//using System.Globalization;
//using System.Linq;
//using System.Threading.Tasks;
//using MedicineStore.Data;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Logging;
//using MustafaviManagementApp.Models;
//using MustafaviManagementApp.ViewModels;  // your DashboardViewModel + DTOs live here

//namespace MustafaviManagementApp.Controllers
//{
//    public class HomeController : Controller
//    {
//        private readonly ILogger<HomeController> _logger;
//        private readonly AppDbContext _db;

//        public HomeController(ILogger<HomeController> logger, AppDbContext db)
//        {
//            _logger = logger;
//            _db = db;
//        }

//        public async Task<IActionResult> Index()
//        {
//            var today = DateTime.Today;
//            var cal = CultureInfo.CurrentCulture.Calendar;

//            // 1) Total ever purchased vs remaining on‐hand
//            var totalStockEver = await _db.PurchaseDetails.SumAsync(pd => pd.Quantity);
//            var remainingStock = await _db.Inventorys.SumAsync(i => i.QuantityOnHand);

//            // 2) Items with stock < 5
//            var lowStockItems = await _db.Inventorys
//                .Where(i => i.QuantityOnHand < 5)
//                .Include(i => i.Medicine)
//                .Select(i => new StockLevelDto
//                {
//                    MedicineName = i.Medicine.MedicineName,
//                    Quantity = i.QuantityOnHand
//                })
//                .ToListAsync();

//            // 3) Daily Sales (last 7 days)
//            var rawDailySales = await _db.Sales
//                .Where(s => !s.IsHeld && s.SaleDate >= today.AddDays(-6))
//                .GroupBy(s => s.SaleDate.Date)
//                .Select(g => new {
//                    Date = g.Key,
//                    Total = g.Sum(s => s.TotalAmount)
//                })
//                .ToListAsync();

//            var dailySales = Enumerable.Range(0, 7).Select(offset => {
//                var d = today.AddDays(-6 + offset);
//                var rec = rawDailySales.FirstOrDefault(x => x.Date == d);
//                return new TimeSeriesPoint
//                {
//                    Period = d.ToString("yyyy-MM-dd"),
//                    Value = rec?.Total ?? 0m
//                };
//            }).ToList();

//            // 3b) Weekly Sales (last 4 weeks) – pull into memory first
//            var recentSales = await _db.Sales
//                .Where(s => !s.IsHeld && s.SaleDate >= today.AddDays(-27))
//                .Select(s => new { s.SaleDate, s.TotalAmount })
//                .ToListAsync();

//            var rawWeeklySales = recentSales
//                .GroupBy(x => cal.GetWeekOfYear(x.SaleDate, CalendarWeekRule.FirstDay, DayOfWeek.Monday))
//                .Select(g => new {
//                    Week = g.Key,
//                    Total = g.Sum(x => x.TotalAmount)
//                })
//                .ToList();

//            var currentWeek = cal.GetWeekOfYear(today, CalendarWeekRule.FirstDay, DayOfWeek.Monday);
//            var weeklySales = Enumerable.Range(0, 4).Select(i => {
//                var wk = currentWeek - 3 + i;
//                var rec = rawWeeklySales.FirstOrDefault(x => x.Week == wk);
//                return new TimeSeriesPoint
//                {
//                    Period = "W" + wk,
//                    Value = rec?.Total ?? 0m
//                };
//            }).ToList();

//            // 4) Daily Purchases (last 7 days)
//            var rawDailyPurch = await _db.Purchases
//                .Where(p => p.PurchaseDate >= today.AddDays(-6))
//                .GroupBy(p => p.PurchaseDate.Date)
//                .Select(g => new {
//                    Date = g.Key,
//                    Total = g.Sum(p => p.TotalCost)
//                })
//                .ToListAsync();

//            var dailyPurchases = Enumerable.Range(0, 7).Select(offset => {
//                var d = today.AddDays(-6 + offset);
//                var rec = rawDailyPurch.FirstOrDefault(x => x.Date == d);
//                return new TimeSeriesPoint
//                {
//                    Period = d.ToString("yyyy-MM-dd"),
//                    Value = rec?.Total ?? 0m
//                };
//            }).ToList();

//            // 4b) Weekly Purchases (last 4 weeks)
//            var recentPurchases = await _db.Purchases
//                .Where(p => p.PurchaseDate >= today.AddDays(-27))
//                .Select(p => new { p.PurchaseDate, p.TotalCost })
//                .ToListAsync();

//            var rawWeeklyPurch = recentPurchases
//                .GroupBy(x => cal.GetWeekOfYear(x.PurchaseDate, CalendarWeekRule.FirstDay, DayOfWeek.Monday))
//                .Select(g => new {
//                    Week = g.Key,
//                    Total = g.Sum(x => x.TotalCost)
//                })
//                .ToList();

//            var weeklyPurchases = Enumerable.Range(0, 4).Select(i => {
//                var wk = currentWeek - 3 + i;
//                var rec = rawWeeklyPurch.FirstOrDefault(x => x.Week == wk);
//                return new TimeSeriesPoint
//                {
//                    Period = "W" + wk,
//                    Value = rec?.Total ?? 0m
//                };
//            }).ToList();

//            // 5) Inventory values & revenue
//            var totalInvValue = await _db.PurchaseDetails
//                .SumAsync(pd => pd.Quantity * pd.CostPrice);

//            var remainingInvValue = await _db.Inventorys
//                .Include(i => i.Medicine)
//                .SumAsync(i => i.QuantityOnHand
//                              * (i.Medicine.SingleUnitPrice ?? i.Medicine.CotanUnitPrice));

//            var revenue = await _db.Sales
//                .Where(s => !s.IsHeld)
//                .SumAsync(s => s.TotalAmount);

//            // assemble DashboardViewModel
//            var vm = new DashboardViewModel
//            {
//                TotalStock = totalStockEver,
//                RemainingStock = remainingStock,
//                LowStockItems = lowStockItems,
//                DailySales = dailySales,
//                WeeklySales = weeklySales,
//                DailyPurchases = dailyPurchases,
//                WeeklyPurchases = weeklyPurchases,
//                TotalInventoryValue = totalInvValue,
//                RemainingInventoryValue = remainingInvValue.Value,
//                Revenue = revenue
//            };

//            return View(vm);
//        }

//        public IActionResult Privacy() => View();

//        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
//        public IActionResult Error() =>
//            View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
//    }
//}
