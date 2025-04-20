using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MedicineStore.Data;
using MedicineStore.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MedicineStore.Controllers
{
    public class InventoriesController : Controller
    {
        private readonly AppDbContext _context;
        public InventoriesController(AppDbContext context) => _context = context;

        // Populate medicine dropdown
        private void PopulateMedicines()
        {
            ViewBag.Medicines = new SelectList(
                _context.Medicines
                        .OrderBy(m => m.MedicineName)
                        .ToList(),
                "MedicineId",
                "MedicineName"
            );
        }

        // GET: Inventories
        public async Task<IActionResult> Index()
        {
            var items = await _context.Inventorys
                .Include(i => i.Medicine)
                .ToListAsync();
            return View(items);
        }

        // GET: Inventories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var inventory = await _context.Inventorys
                .Include(i => i.Medicine)
                .FirstOrDefaultAsync(i => i.InventoryId == id);
            if (inventory == null) return NotFound();
            return View(inventory);
        }

        // GET: Inventories/Create
        public IActionResult Create()
        {
            PopulateMedicines();
            return View(new Inventory { CreatedAt = DateTime.Now });
        }

        // POST: Inventories/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Inventory inventory)
        {
            PopulateMedicines();
            if (!ModelState.IsValid)
                return View(inventory);

            inventory.CreatedAt = DateTime.Now;
            _context.Add(inventory);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Inventories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var inventory = await _context.Inventorys.FindAsync(id);
            if (inventory == null) return NotFound();

            PopulateMedicines();
            return View(inventory);
        }

        // POST: Inventories/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Inventory inventory)
        {
            if (id != inventory.InventoryId) return NotFound();

            PopulateMedicines();
            if (!ModelState.IsValid)
                return View(inventory);

            try
            {
                inventory.UpdatedAt = DateTime.Now;
                _context.Update(inventory);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Inventorys.Any(e => e.InventoryId == id))
                    return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Inventories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var inventory = await _context.Inventorys
                .Include(i => i.Medicine)
                .FirstOrDefaultAsync(i => i.InventoryId == id);
            if (inventory == null) return NotFound();
            return View(inventory);
        }

        // POST: Inventories/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var inventory = await _context.Inventorys.FindAsync(id);
            if (inventory != null)
            {
                _context.Inventorys.Remove(inventory);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
