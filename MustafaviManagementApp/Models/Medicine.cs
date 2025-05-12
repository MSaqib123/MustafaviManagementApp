using MedicineStore.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations.Schema;

public class Medicine
{
    public Medicine()
    {
        Inventories = new List<Inventory>();
        PurchaseDetails = new List<PurchaseDetail>();
        SaleDetails = new List<SaleDetail>();
    }

    public int MedicineId { get; set; }

    public int? ParentCategoryId { get; set; }  // Parent category for filtering
    public int CategoryId { get; set; }        // Was ChildCategoryId (renamed)

    public int StoreId { get; set; }

    public string MedicineName { get; set; }
    public string? DosageForm { get; set; }
    public string? Strength { get; set; }
    public int ReorderLevel { get; set; }

    public string PriceType { get; set; } // SINGLE, COTAN, BOTH
    public decimal? SingleUnitPrice { get; set; }
    public decimal? CotanUnitPrice { get; set; }
    public int? CotanUnitSize { get; set; }

    public string? Image { get; set; }
    public string? UrduName { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    [ValidateNever]
    public ParentCategory ParentCategory { get; set; }  // Parent Category navigation

    [ValidateNever]
    public Category Category { get; set; }        // Child Category navigation (renamed from ChildCategory)

    [ValidateNever]
    public Store Store { get; set; }

    [ValidateNever]
    public ICollection<Inventory> Inventories { get; set; }

    [ValidateNever]
    public ICollection<PurchaseDetail> PurchaseDetails { get; set; }

    [ValidateNever]
    public ICollection<SaleDetail> SaleDetails { get; set; }

    [NotMapped]
    public IFormFile? ImageFile { get; set; }
}
