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
    public class SuppliersController : Controller
    {
        private readonly AppDbContext _context;
        public SuppliersController(AppDbContext context) => _context = context;

        // Populate the store dropdown
        private void PopulateStores()
        {
            ViewBag.Stores = new SelectList(
                _context.Stores
                        .OrderBy(s => s.StoreName)
                        .ToList(),
                "StoreId", "StoreName"
            );
        }

        // GET: Suppliers
        public async Task<IActionResult> Index()
        {
            var suppliers = await _context.Suppliers
                .Include(s => s.Store)
                .ToListAsync();
            return View(suppliers);
        }

        // GET: Suppliers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var supplier = await _context.Suppliers
                .Include(s => s.Store)
                .FirstOrDefaultAsync(s => s.SupplierId == id);
            if (supplier == null) return NotFound();
            return View(supplier);
        }

        // GET: Suppliers/Create
        public IActionResult Create()
        {
            PopulateStores();
            return View(new Supplier());
        }

        // POST: Suppliers/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Supplier supplier)
        {
            PopulateStores();
            if (!ModelState.IsValid)
                return View(supplier);

            supplier.CreatedAt = DateTime.Now;
            _context.Add(supplier);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Suppliers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null) return NotFound();

            PopulateStores();
            return View(supplier);
        }

        // POST: Suppliers/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Supplier supplier)
        {
            if (id != supplier.SupplierId) return NotFound();

            PopulateStores();
            if (!ModelState.IsValid)
                return View(supplier);

            try
            {
                supplier.UpdatedAt = DateTime.Now;
                _context.Update(supplier);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Suppliers.Any(e => e.SupplierId == id))
                    return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Suppliers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var supplier = await _context.Suppliers
                .Include(s => s.Store)
                .FirstOrDefaultAsync(s => s.SupplierId == id);
            if (supplier == null) return NotFound();
            return View(supplier);
        }

        // POST: Suppliers/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier != null)
            {
                _context.Suppliers.Remove(supplier);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
