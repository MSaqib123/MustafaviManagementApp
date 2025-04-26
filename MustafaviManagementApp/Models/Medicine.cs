using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;

namespace MedicineStore.Models
{
    public class Medicine
    {
        public Medicine()
        {
            Inventories = new List<Inventory>();
            PurchaseDetails = new List<PurchaseDetail>();
            SaleDetails = new List<SaleDetail>();
        }
        public int MedicineId { get; set; }
        public int CategoryId { get; set; }
        public int StoreId { get; set; }
        public string MedicineName { get; set; }
        public string? Brand { get; set; }
        public string? DosageForm { get; set; }
        public string? Strength { get; set; }
        public int ReorderLevel { get; set; }
        public string PriceType { get; set; } // SINGLE, COTAN, BOTH
        public decimal? SingleUnitPrice { get; set; }
        public decimal? CotanUnitPrice { get; set; }
        public int? CotanUnitSize { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        [ValidateNever]
        public Category Category { get; set; }
        [ValidateNever]
        public Store Store { get; set; }
        [ValidateNever]
        public ICollection<Inventory> Inventories { get; set; }
        [ValidateNever]
        public ICollection<PurchaseDetail> PurchaseDetails { get; set; }
        [ValidateNever]
        public ICollection<SaleDetail> SaleDetails { get; set; }
    }

}