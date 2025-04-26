namespace MustafaviManagementApp.ViewModels
{

    public record POSLineItem(
        int MedicineId,
        string MedicineName,
        int Quantity,
        decimal UnitPrice,
        decimal Discount,
        string BatchNumber,
        DateTime? ExpiryDate
    )
    {
        public decimal SubTotal => Quantity * UnitPrice - Discount;
    }


}
