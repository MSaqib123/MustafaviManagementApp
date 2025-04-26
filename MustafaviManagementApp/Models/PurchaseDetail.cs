using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;

namespace MedicineStore.Models
{
    public class PurchaseDetail
    {
        public int PurchaseDetailId { get; set; }
        public int PurchaseId { get; set; }
        public int MedicineId { get; set; }
        public string? BatchNumber { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int Quantity { get; set; }
        public decimal CostPrice { get; set; }
        public decimal Discount { get; set; }

        public decimal SubTotal => (Quantity * CostPrice - Discount);
        [ValidateNever]
        public Purchase Purchase { get; set; }
        [ValidateNever]
        public Medicine Medicine { get; set; }
    }

}