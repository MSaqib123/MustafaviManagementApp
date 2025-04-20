using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;

namespace MedicineStore.Models
{
    public class DailySummary
    {
        public int DailySummaryId { get; set; }
        public DateTime SummaryDate { get; set; }
        public int StoreId { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalPurchases { get; set; }
        public decimal NetProfit => TotalSales - TotalPurchases;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        [ValidateNever]
        public Store Store { get; set; }
    }

}