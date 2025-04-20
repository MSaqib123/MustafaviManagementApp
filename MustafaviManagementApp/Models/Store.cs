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

        public ICollection<Staff> Staffs { get; set; }
        public ICollection<Supplier> Suppliers { get; set; }
        public ICollection<Medicine> Medicines { get; set; }
        public ICollection<Customer> Customers { get; set; }
        public ICollection<DailySummary> DailySummaries { get; set; }
    }

}