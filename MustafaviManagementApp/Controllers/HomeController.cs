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




        /* ──────────────────────────────── DASHBOARD ─────────────────────────────── */
        public async Task<IActionResult> Index(string period = "daily")
        {
            /* ░░ 0.  Pass current period to View ░░ */
            period = (period ?? "daily").ToLowerInvariant();
            ViewBag.Period = period;

            var today = DateTime.Today;
            var cal = CultureInfo.CurrentCulture.Calendar;

            /* ░░ 1.  Stock numbers (unchanged) ░░ */
            var totalStockEver = await _db.StockLedgers.Where(l => l.QtyChange > 0)
                                     .SumAsync(l => (int?)l.QtyChange) ?? 0;
            var latestBalances = await _db.StockLedgers
                .GroupBy(l => l.MedicineId)
                .Select(g => g.OrderByDescending(l => l.StockLedgerId).First())
                .ToListAsync();

            var remainingStock = latestBalances.Sum(l => l.BalanceAfter);

            var lowStockItems = latestBalances
                .Where(l => l.BalanceAfter < 5)
                .Join(_db.Medicines,
                      ledg => ledg.MedicineId,
                      med => med.MedicineId,
                      (ledg, med) => new StockLevelDto
                      {
                          MedicineName = med.MedicineName,
                          Quantity = ledg.BalanceAfter
                      })
                .OrderBy(x => x.Quantity).ToList();

            /* ░░ 2.  Helper lambdas ░░ */
            TimeSeriesPoint P(string lbl, decimal v) => new() { Period = lbl, Value = v };

            string lblDay(DateTime d) => d.ToString("yyyy-MM-dd");
            string lblWeek(int w) => "W" + w;
            string lblMonth(DateTime d) => d.ToString("yyyy-MM");
            string lblYear(int y) => y.ToString();

            /* pull once – we’ll post-filter in memory */
            var sales = await _db.Sales
                .Where(s => !s.IsHeld)
                .Select(s => new { s.SaleDate, s.TotalAmount })
                .ToListAsync();

            var purch = await _db.Purchases
                .Select(p => new { p.PurchaseDate, p.TotalCost })
                .ToListAsync();

            /* initialise lists (empty by default) */
            var dSales = new List<TimeSeriesPoint>();
            var wSales = new List<TimeSeriesPoint>();
            var dPurch = new List<TimeSeriesPoint>();
            var wPurch = new List<TimeSeriesPoint>();

            /* ░░ 3.  Build series according to period ░░ */
            switch (period)
            {
                /* ───── DAILY: last 7 days ───── */
                case "daily":
                default:
                    for (int i = -6; i <= 0; i++)
                    {
                        var d = today.AddDays(i);
                        dSales.Add(P(lblDay(d),
                             sales.Where(x => x.SaleDate.Date == d).Sum(x => x.TotalAmount)));
                        dPurch.Add(P(lblDay(d),
                             purch.Where(x => x.PurchaseDate.Date == d).Sum(x => x.TotalCost)));
                    }
                    /* plus weekly summaries for the last 4 weeks */
                    int curWeek = cal.GetWeekOfYear(today,
                                    CalendarWeekRule.FirstDay, DayOfWeek.Monday);
                    for (int wk = curWeek - 3; wk <= curWeek; wk++)
                    {
                        wSales.Add(P(lblWeek(wk),
                            sales.Where(x => cal.GetWeekOfYear(x.SaleDate,
                                       CalendarWeekRule.FirstDay, DayOfWeek.Monday) == wk)
                                 .Sum(x => x.TotalAmount)));
                        wPurch.Add(P(lblWeek(wk),
                            purch.Where(x => cal.GetWeekOfYear(x.PurchaseDate,
                                       CalendarWeekRule.FirstDay, DayOfWeek.Monday) == wk)
                                 .Sum(x => x.TotalCost)));
                    }
                    break;

                /* ───── WEEKLY: last 12 calendar weeks ───── */
                case "weekly":
                    int endW = cal.GetWeekOfYear(today, CalendarWeekRule.FirstDay, DayOfWeek.Monday);
                    int startW = endW - 11;
                    for (int wk = startW; wk <= endW; wk++)
                    {
                        wSales.Add(P(lblWeek(wk),
                            sales.Where(x => cal.GetWeekOfYear(x.SaleDate,
                                       CalendarWeekRule.FirstDay, DayOfWeek.Monday) == wk)
                                 .Sum(x => x.TotalAmount)));
                        wPurch.Add(P(lblWeek(wk),
                            purch.Where(x => cal.GetWeekOfYear(x.PurchaseDate,
                                       CalendarWeekRule.FirstDay, DayOfWeek.Monday) == wk)
                                 .Sum(x => x.TotalCost)));
                    }
                    break;

                /* ───── MONTHLY: last 12 months ───── */
                case "monthly":
                    var firstMonth = new DateTime(today.Year, today.Month, 1).AddMonths(-11);
                    for (int i = 0; i < 12; i++)
                    {
                        var m = firstMonth.AddMonths(i);
                        dSales.Add(P(lblMonth(m),
                            sales.Where(x => x.SaleDate.Year == m.Year &&
                                             x.SaleDate.Month == m.Month)
                                 .Sum(x => x.TotalAmount)));
                        dPurch.Add(P(lblMonth(m),
                            purch.Where(x => x.PurchaseDate.Year == m.Year &&
                                             x.PurchaseDate.Month == m.Month)
                                 .Sum(x => x.TotalCost)));
                    }
                    break;

                /* ───── YEARLY (10y) & LAST5 (5y) ───── */
                case "yearly":
                case "last5":
                    int span = period == "last5" ? 5 : 10;
                    int firstYr = today.Year - span + 1;
                    for (int y = firstYr; y <= today.Year; y++)
                    {
                        dSales.Add(P(lblYear(y),
                            sales.Where(x => x.SaleDate.Year == y).Sum(x => x.TotalAmount)));
                        dPurch.Add(P(lblYear(y),
                            purch.Where(x => x.PurchaseDate.Year == y).Sum(x => x.TotalCost)));
                    }
                    break;
            }

            /* ░░ 4.  Inventory value & revenue ░░ */
            var totalInvValue = await _db.PurchaseDetails
                .SumAsync(pd => pd.Quantity * pd.CostPrice);

            var remainingInvValue = (from l in latestBalances
                                     join m in _db.Medicines on l.MedicineId equals m.MedicineId
                                     let price = m.SingleUnitPrice ?? m.CotanUnitPrice ?? 0m
                                     select price * l.BalanceAfter).Sum();

            var revenue = await _db.Sales.Where(s => !s.IsHeld)
                             .SumAsync(s => s.TotalAmount);

            /* ░░ 5.  Pack ViewModel ░░ */
            var vm = new DashboardViewModel
            {
                TotalStock = totalStockEver,
                RemainingStock = remainingStock,
                LowStockItems = lowStockItems,
                DailySales = dSales,
                WeeklySales = wSales,
                DailyPurchases = dPurch,
                WeeklyPurchases = wPurch,
                TotalInventoryValue = totalInvValue,
                RemainingInventoryValue = remainingInvValue,
                Revenue = revenue
            };
            return View(vm);
        }




        //    public async Task<IActionResult> Index()
        //    {
        //        var today = DateTime.Today;
        //        var cal = CultureInfo.CurrentCulture.Calendar;

        //        /* ───────────────────────────────────────────────
        //           1) Stock numbers – pure StockLedger
        //           ─────────────────────────────────────────────── */

        //        // 1-a  total units ever IN (positive qtyChange only)
        //        var totalStockEver = await _db.StockLedgers
        //                               .Where(l => l.QtyChange > 0)
        //                               .SumAsync(l => (int?)l.QtyChange) ?? 0;

        //        // 1-b  latest balance per medicine → on-hand
        //        var latestBalances = await _db.StockLedgers
        //                                .GroupBy(l => l.MedicineId)
        //                                .Select(g => g.OrderByDescending(l => l.StockLedgerId)
        //                                              .FirstOrDefault())
        //                                .ToListAsync();
        //        var remainingStock = latestBalances.Sum(l => l?.BalanceAfter ?? 0);

        //        // 1-c  low-stock (<5) list
        //        var lowStockItems = latestBalances
        //            .Where(l => l != null && l.BalanceAfter < 5)
        //            .Join(_db.Medicines,
        //                  ledg => ledg.MedicineId,
        //                  med => med.MedicineId,
        //                  (ledg, med) => new StockLevelDto
        //                  {
        //                      MedicineName = med.MedicineName,
        //                      Quantity = ledg.BalanceAfter
        //                  })
        //            .OrderBy(x => x.Quantity)
        //            .ToList();

        //        /* ───────────────────────────────────────────────
        //           2) Sales & Purchases (unchanged)
        //           ─────────────────────────────────────────────── */
        //        /* ----------- time-series helpers ----------- */
        //        TimeSeriesPoint MapSeries(DateTime d, decimal? total) =>
        //            new() { Period = d.ToString("yyyy-MM-dd"), Value = total ?? 0m };

        //        /* 2-a  Daily Sales last 7d */
        //        var rawDailySales = await _db.Sales
        //            .Where(s => !s.IsHeld && s.SaleDate >= today.AddDays(-6))
        //            .GroupBy(s => s.SaleDate.Date)
        //            .Select(g => new { Date = g.Key, Total = g.Sum(s => s.TotalAmount) })
        //            .ToListAsync();

        //        var dailySales = Enumerable.Range(0, 7)
        //            .Select(i => {
        //                var d = today.AddDays(-6 + i);
        //                var rec = rawDailySales.FirstOrDefault(x => x.Date == d);
        //                return MapSeries(d, rec?.Total);
        //            }).ToList();

        //        /* 2-b  Weekly Sales last 4w */
        //        var recentSales = await _db.Sales
        //            .Where(s => !s.IsHeld && s.SaleDate >= today.AddDays(-27))
        //            .Select(s => new { s.SaleDate, s.TotalAmount })
        //            .ToListAsync();

        //        var rawWeeklySales = recentSales
        //            .GroupBy(x => cal.GetWeekOfYear(x.SaleDate,
        //                     CalendarWeekRule.FirstDay, DayOfWeek.Monday))
        //            .Select(g => new { Week = g.Key, Total = g.Sum(x => x.TotalAmount) })
        //            .ToList();

        //        var curWk = cal.GetWeekOfYear(today,
        //                      CalendarWeekRule.FirstDay, DayOfWeek.Monday);
        //        var weeklySales = Enumerable.Range(0, 4).Select(i => {
        //            var wk = curWk - 3 + i;
        //            var rec = rawWeeklySales.FirstOrDefault(x => x.Week == wk);
        //            return new TimeSeriesPoint { Period = "W" + wk, Value = rec?.Total ?? 0m };
        //        }).ToList();

        //        /* 2-c  Daily Purchases last 7d */
        //        var rawDailyPurch = await _db.Purchases
        //            .Where(p => p.PurchaseDate >= today.AddDays(-6))
        //            .GroupBy(p => p.PurchaseDate.Date)
        //            .Select(g => new { Date = g.Key, Total = g.Sum(p => p.TotalCost) })
        //            .ToListAsync();

        //        var dailyPurch = Enumerable.Range(0, 7).Select(i => {
        //            var d = today.AddDays(-6 + i);
        //            var rec = rawDailyPurch.FirstOrDefault(x => x.Date == d);
        //            return MapSeries(d, rec?.Total);
        //        }).ToList();

        //        /* 2-d  Weekly Purchases last 4w */
        //        var recentPurch = await _db.Purchases
        //            .Where(p => p.PurchaseDate >= today.AddDays(-27))
        //            .Select(p => new { p.PurchaseDate, p.TotalCost })
        //            .ToListAsync();

        //        var rawWeeklyPurch = recentPurch
        //            .GroupBy(x => cal.GetWeekOfYear(x.PurchaseDate,
        //                     CalendarWeekRule.FirstDay, DayOfWeek.Monday))
        //            .Select(g => new { Week = g.Key, Total = g.Sum(x => x.TotalCost) })
        //            .ToList();

        //        var weeklyPurch = Enumerable.Range(0, 4).Select(i => {
        //            var wk = curWk - 3 + i;
        //            var rec = rawWeeklyPurch.FirstOrDefault(x => x.Week == wk);
        //            return new TimeSeriesPoint { Period = "W" + wk, Value = rec?.Total ?? 0m };
        //        }).ToList();

        //        /* ───────────────────────────────────────────── */
        //        /* 3) Inventory value + revenue (unchanged)    */
        //        /* ───────────────────────────────────────────── */
        //        var totalInvValue = await _db.PurchaseDetails
        //            .SumAsync(pd => pd.Quantity * pd.CostPrice);

        //        var remainingInvValue = remainingStock == 0
        //             ? 0m
        //             : latestBalances
        //                 .Join(_db.Medicines, l => l.MedicineId, m => m.MedicineId,
        //                       (l, m) => (decimal)l.BalanceAfter *
        //                               (m.SingleUnitPrice ?? m.CotanUnitPrice ?? 0m))
        //                 .Sum();

        //        var revenue = await _db.Sales
        //            .Where(s => !s.IsHeld)
        //            .SumAsync(s => s.TotalAmount);

        //        /* 4) Build ViewModel */
        //        var vm = new DashboardViewModel
        //        {
        //            TotalStock = totalStockEver,
        //            RemainingStock = remainingStock,
        //            LowStockItems = lowStockItems,
        //            DailySales = dailySales,
        //            WeeklySales = weeklySales,
        //            DailyPurchases = dailyPurch,
        //            WeeklyPurchases = weeklyPurch,
        //            TotalInventoryValue = totalInvValue,
        //            RemainingInventoryValue = remainingInvValue,
        //            Revenue = revenue
        //        };
        //        return View(vm);
        //    }

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
