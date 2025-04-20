using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedicineStore.Data;
using MedicineStore.Models;
using System.Threading.Tasks;
using System.Linq;

namespace MedicineStore.Controllers
{
    public class DailySummarysController : Controller
    {
        private readonly AppDbContext _context;
        public DailySummarysController(AppDbContext context) => _context = context;

        public async Task<IActionResult> Index() => View(await _context.DailySummarys.ToListAsync());

        public async Task<IActionResult> Details(int? id)
        {{
            if (id == null) return NotFound();
            var item = await _context.DailySummarys.FirstOrDefaultAsync(m => m.DailySummaryId == id);
            if (item == null) return NotFound();
            return View(item);
        }}

        public IActionResult Create() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DailySummary item)
        {{
            if (!ModelState.IsValid) return View(item);
            _context.Add(item);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }}

        public async Task<IActionResult> Edit(int? id)
        {{
            if (id == null) return NotFound();
            var item = await _context.DailySummarys.FindAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }}

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DailySummary item)
        {{
            if (id != item.DailySummaryId) return NotFound();
            if (!ModelState.IsValid) return View(item);
            _context.Update(item);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }}

        public async Task<IActionResult> Delete(int? id)
        {{
            if (id == null) return NotFound();
            var item = await _context.DailySummarys.FirstOrDefaultAsync(m => m.DailySummaryId == id);
            if (item == null) return NotFound();
            return View(item);
        }}

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {{
            var item = await _context.DailySummarys.FindAsync(id);
            _context.DailySummarys.Remove(item);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }}
    }
}