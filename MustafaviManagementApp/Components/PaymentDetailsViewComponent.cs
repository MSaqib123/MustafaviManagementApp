// /ViewComponents/SaleDetailsViewComponent.cs
using MedicineStore.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

public class PaymentDetailsViewComponent : ViewComponent
{
    private readonly AppDbContext _db;
    public PaymentDetailsViewComponent(AppDbContext db) => _db = db;

    public async Task<IViewComponentResult> InvokeAsync(int id)
    {
        var details = await _db.PaymentDetails
            .Where(d => d.PaymentId == id)
            .ToListAsync();

        return View(details);
    }
}
