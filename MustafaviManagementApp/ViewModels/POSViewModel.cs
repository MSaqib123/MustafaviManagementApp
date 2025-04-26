namespace MustafaviManagementApp.ViewModels
{

    public class POSViewModel
    {
        public int? CustomerId { get; set; }
        public int StaffId { get; set; }
        public string Currency { get; set; } = "PKR";
        public decimal CurrencyRate { get; set; } = 1m;    // 1 = PKR base
        public decimal OverallDiscount { get; set; }
        public bool DiscountIsPercent { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
        public string PaymentStatus { get; set; } = "Pending"; // Paid | Pending
        public List<POSLineItem> Items { get; set; } = new();
        public decimal VATPercent { get; set; } = 0m;

        public decimal Gross => Items.Sum(x => x.SubTotal);
        public decimal DiscountValue => DiscountIsPercent ? Gross * (OverallDiscount / 100m) : OverallDiscount;
        public decimal Net => Gross - DiscountValue;
        public decimal VAT => Net * (VATPercent / 100m);
        public decimal GrandTotal => Net + VAT;
        public int TotalQty => Items.Sum(x => x.Quantity);
    }
}
