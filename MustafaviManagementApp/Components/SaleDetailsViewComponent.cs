// /ViewComponents/SaleDetailsViewComponent.cs
using MedicineStore.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class SaleDetailsViewComponent : ViewComponent
{
    private readonly AppDbContext _db;
    public SaleDetailsViewComponent(AppDbContext db) => _db = db;

    public async Task<IViewComponentResult> InvokeAsync(int saleId)
    {
        var details = await _db.SaleDetails
            .Where(d => d.SaleId == saleId)
            .Include(d => d.Medicine)
            .ToListAsync();

        return View(details);
    }
}
