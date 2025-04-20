using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;

namespace MedicineStore.Models
{
    public class Sale
    {
        public int SaleId { get; set; }
        public int? CustomerId { get; set; }
        public int StaffId { get; set; }
        public DateTime SaleDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal Discount { get; set; }
        public string PaymentMethod { get; set; }
        public string PaymentStatus { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        [ValidateNever]
        public Customer? Customer { get; set; }
        [ValidateNever]
        public Staff Staff { get; set; }

        public ICollection<SaleDetail> SaleDetails { get; set; }
        public ICollection<Prescription> Prescriptions { get; set; }
    }

}