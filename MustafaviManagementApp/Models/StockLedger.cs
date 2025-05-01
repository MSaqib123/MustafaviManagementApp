using MedicineStore.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace MustafaviManagementApp.Models
{
    public class StockLedger
    {
        public int StockLedgerId { get; set; }
        public int MedicineId { get; set; }
        public int? SaleId { get; set; }      // NULL جب purchase یا manual adj.
        public int? PurchaseId { get; set; }
        public string ActionType { get; set; }      // IN | OUT | RESERVE | RELEASE
        public int QtyChange { get; set; }      // +ve for IN / RELEASE , -ve for OUT / RESERVE
        public int BalanceAfter { get; set; }      // OPTIONAL: فوری بیلنس
        public int? QtyBeforeChange { get; set; } 

        public DateTime CreatedAt { get; set; }

        /* ─ navigation ─ */
        [ValidateNever] public Medicine Medicine { get; set; }
        [ValidateNever] public Sale? Sale { get; set; }
        [ValidateNever] public Purchase? Purchase { get; set; }
    }
}
