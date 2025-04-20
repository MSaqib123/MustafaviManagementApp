namespace MustafaviManagementApp.ViewModels
{

    public class POSLineItem
    {
        public int MedicineId { get; set; }
        public string MedicineName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Discount { get; set; }
        public decimal SubTotal => Quantity * UnitPrice - Discount;
    }
}
