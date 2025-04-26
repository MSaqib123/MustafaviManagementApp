using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;

namespace MedicineStore.Models
{
    public class Payment
    {
        public int PaymentId { get; set; }
        public string ReferenceType { get; set; } // "Sale" or "Purchase"
        public int ReferenceId { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public DateTime PaymentDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        [ValidateNever]
        public ICollection<PaymentDetail> PaymentDetails { get; set; }
    }

}