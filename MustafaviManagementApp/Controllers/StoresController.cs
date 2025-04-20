using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedicineStore.Data;
using MedicineStore.Models;
using System.Threading.Tasks;
using System.Linq;

namespace MedicineStore.Controllers
{
    public class StoresController : Controller
    {
        private readonly AppDbContext _context;
        public StoresController(AppDbContext context) => _context = context;

        public async Task<IActionResult> Index() => View(await _context.Stores.ToListAsync());

        public async Task<IActionResult> Details(int? id)
        {{
            if (id == null) return NotFound();
            var item = await _context.Stores.FirstOrDefaultAsync(m => m.StoreId == id);
            if (item == null) return NotFound();
            return View(item);
        }}

        public IActionResult Create() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Store item)
        {{
            if (!ModelState.IsValid) return View(item);
            _context.Add(item);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }}

        public async Task<IActionResult> Edit(int? id)
        {{
            if (id == null) return NotFound();
            var item = await _context.Stores.FindAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }}

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Store item)
        {{
            if (id != item.StoreId) return NotFound();
            if (!ModelState.IsValid) return View(item);
            _context.Update(item);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }}

        public async Task<IActionResult> Delete(int? id)
        {{
            if (id == null) return NotFound();
            var item = await _context.Stores.FirstOrDefaultAsync(m => m.StoreId == id);
            if (item == null) return NotFound();
            return View(item);
        }}

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {{
            var item = await _context.Stores.FindAsync(id);
            _context.Stores.Remove(item);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }}
    }
}
