using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedicineStore.Data;
using MedicineStore.Models;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MedicineStore.Controllers
{
    public class MedicinesController : Controller
    {
        private readonly AppDbContext _context;

        public MedicinesController(AppDbContext context)
            => _context = context;

        // Helper to populate the dropdowns
        private void PopulateDropdowns()
        {
            ViewBag.Categories = new SelectList(
                _context.Categorys
                        .OrderBy(c => c.CategoryName)
                        .ToList(),                // <-- materialize here
                "CategoryId",
                "CategoryName"
            );
            ViewBag.Stores = new SelectList(
                _context.Stores
                        .OrderBy(s => s.StoreName)
                        .ToList(),                // <-- materialize here
                "StoreId",
                "StoreName"
            );
        }

        // GET: Medicines
        public async Task<IActionResult> Index()
        {
            var meds = await _context.Medicines
                .Include(m => m.Category)
                .Include(m => m.Store)
                .ToListAsync();
            return View(meds);
        }

        // GET: Medicines/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var med = await _context.Medicines
                .Include(m => m.Category)
                .Include(m => m.Store)
                .FirstOrDefaultAsync(m => m.MedicineId == id);

            if (med == null) return NotFound();
            return View(med);
        }

        // GET: Medicines/Create
        public IActionResult Create()
        {
            Medicine m = new Medicine();
            PopulateDropdowns();
            return View(m);
        }

        // POST: Medicines/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Medicine item)
        {
            PopulateDropdowns();
            if (!ModelState.IsValid)
                return View(item);

            item.CreatedAt = DateTime.Now;
            _context.Add(item);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Medicines/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var item = await _context.Medicines.FindAsync(id);
            if (item == null) return NotFound();

            PopulateDropdowns();
            return View(item);
        }

        // POST: Medicines/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Medicine item)
        {
            PopulateDropdowns();
            if (id != item.MedicineId) return NotFound();
            if (!ModelState.IsValid) return View(item);

            try
            {
                item.UpdatedAt = DateTime.Now;
                _context.Update(item);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Medicines.Any(e => e.MedicineId == id))
                    return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Medicines/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var item = await _context.Medicines
                .Include(m => m.Category)
                .Include(m => m.Store)
                .FirstOrDefaultAsync(m => m.MedicineId == id);

            if (item == null) return NotFound();
            return View(item);
        }

        // POST: Medicines/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _context.Medicines.FindAsync(id);
            _context.Medicines.Remove(item!);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
