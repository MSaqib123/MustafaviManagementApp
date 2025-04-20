namespace MustafaviManagementApp.ViewModels
{

    public class POSViewModel
    {
        public int? CustomerId { get; set; }
        public int StaffId { get; set; }
        public decimal OverallDiscount { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
        public string PaymentStatus { get; set; } = "Pending";
        public List<POSLineItem> Items { get; set; } = new();
        public decimal GrandTotal => Items.Sum(i => i.SubTotal) - OverallDiscount;
    }
}
