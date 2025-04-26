using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;

namespace MedicineStore.Models
{
    public class Supplier
    {
        public int SupplierId { get; set; }
        public int StoreId { get; set; }
        public string SupplierName { get; set; }
        public string? ContactPerson { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        [ValidateNever]
        public Store Store { get; set; }
        [ValidateNever]
        public ICollection<Purchase> Purchases { get; set; }
    }

}