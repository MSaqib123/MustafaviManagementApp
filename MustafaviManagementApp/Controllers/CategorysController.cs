using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedicineStore.Data;
using MedicineStore.Models;
using System.Threading.Tasks;
using System.Linq;

namespace MedicineStore.Controllers
{
    public class CategorysController : Controller
    {
        private readonly AppDbContext _context;
        public CategorysController(AppDbContext context) => _context = context;

        public async Task<IActionResult> Index() => View(await _context.Categorys.ToListAsync());

        public async Task<IActionResult> Details(int? id)
        {{
            if (id == null) return NotFound();
            var item = await _context.Categorys.FirstOrDefaultAsync(m => m.CategoryId == id);
            if (item == null) return NotFound();
            return View(item);
        }}

        public IActionResult Create() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category item)
        {{
            if (!ModelState.IsValid) return View(item);
            _context.Add(item);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }}

        public async Task<IActionResult> Edit(int? id)
        {{
            if (id == null) return NotFound();
            var item = await _context.Categorys.FindAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }}

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category item)
        {{
            if (id != item.CategoryId) return NotFound();
            if (!ModelState.IsValid) return View(item);
            _context.Update(item);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }}

        public async Task<IActionResult> Delete(int? id)
        {{
            if (id == null) return NotFound();
            var item = await _context.Categorys.FirstOrDefaultAsync(m => m.CategoryId == id);
            if (item == null) return NotFound();
            return View(item);
        }}

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {{
            var item = await _context.Categorys.FindAsync(id);
            _context.Categorys.Remove(item);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }}
    }
}