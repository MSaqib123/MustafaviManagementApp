using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;

namespace MedicineStore.Models
{
    public class Inventory
    {
        public int InventoryId { get; set; }
        public int MedicineId { get; set; }
        public string? BatchNumber { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int QuantityOnHand { get; set; }
        public string? LocationInStore { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        [ValidateNever]
        public Medicine Medicine { get; set; }
    }

}