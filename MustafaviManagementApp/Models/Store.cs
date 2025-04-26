using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;

namespace MedicineStore.Models
{
    public class Store
    {
        public int StoreId { get; set; }
        public string StoreName { get; set; }
        public string? Location { get; set; }
        public string? ContactInfo { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        [ValidateNever]
        public ICollection<Staff> Staffs { get; set; }
        [ValidateNever]
        public ICollection<Supplier> Suppliers { get; set; }
        [ValidateNever]
        public ICollection<Medicine> Medicines { get; set; }
        [ValidateNever]
        public ICollection<Customer> Customers { get; set; }
        [ValidateNever]
        public ICollection<DailySummary> DailySummaries { get; set; }
    }

}