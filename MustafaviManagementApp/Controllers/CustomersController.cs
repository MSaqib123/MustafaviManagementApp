using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedicineStore.Data;
using MedicineStore.Models;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MedicineStore.Controllers
{
    public class CustomersController : Controller
    {
        private readonly AppDbContext _context;
        public CustomersController(AppDbContext context) => _context = context;


        // Helper to populate the dropdowns
        private void PopulateDropdowns()
        {
            //ViewBag.Categories = new SelectList(
            //    _context.Categorys
            //            .OrderBy(c => c.CategoryName)
            //            .ToList(),                // <-- materialize here
            //    "CategoryId",
            //    "CategoryName"
            //);
            ViewBag.Stores = new SelectList(
                _context.Stores
                        .OrderBy(s => s.StoreName)
                        .ToList(),                // <-- materialize here
                "StoreId",
                "StoreName"
            );
        }


        public async Task<IActionResult> Index() => View(await _context.Customers.ToListAsync());

        public async Task<IActionResult> Details(int? id)
        {{
            if (id == null) return NotFound();
            var item = await _context.Customers.FirstOrDefaultAsync(m => m.CustomerId == id);
            if (item == null) return NotFound();
            return View(item);
        }}

        public IActionResult Create()
        {
            PopulateDropdowns();
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer item)
        {{
            PopulateDropdowns();
            if (!ModelState.IsValid) return View(item);
            _context.Add(item);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }}

        public async Task<IActionResult> Edit(int? id)
        {{
            if (id == null) return NotFound();
            PopulateDropdowns();
            var item = await _context.Customers.FindAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }}

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Customer item)
        {{
                PopulateDropdowns();
            if (id != item.CustomerId) return NotFound();
            if (!ModelState.IsValid) return View(item);
            _context.Update(item);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }}

        public async Task<IActionResult> Delete(int? id)
        {{
                PopulateDropdowns();
            if (id == null) return NotFound();
            var item = await _context.Customers.FirstOrDefaultAsync(m => m.CustomerId == id);
            if (item == null) return NotFound();
            return View(item);
        }}

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {{
                PopulateDropdowns();
                var item = await _context.Customers.FindAsync(id);
            _context.Customers.Remove(item);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }}
    }
}
