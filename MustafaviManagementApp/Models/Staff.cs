using System;
using System.Collections.Generic;

namespace MedicineStore.Models
{
    public class Staff
    {
        public int StaffId { get; set; }
        public int StoreId { get; set; }
        public string StaffName { get; set; }
        public string? Role { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public decimal? Salary { get; set; }
        public string? Address { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public Store Store { get; set; }
        public ICollection<Purchase> Purchases { get; set; }
        public ICollection<Sale> Sales { get; set; }
    }

}