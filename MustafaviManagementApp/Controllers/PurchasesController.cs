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
    public class PurchasesController : Controller
    {
        private readonly AppDbContext _context;
        public PurchasesController(AppDbContext context) => _context = context;

        // Populate suppliers and staff dropdowns
        private void PopulateDropdowns()
        {
            ViewBag.Suppliers = new SelectList(
                _context.Suppliers.OrderBy(s => s.SupplierName).ToList(),
                "SupplierId", "SupplierName"
            );
            ViewBag.Staffs = new SelectList(
                _context.Staffs.OrderBy(s => s.StaffName).ToList(),
                "StaffId", "StaffName"
            );
        }

        // GET: Purchases
        public async Task<IActionResult> Index()
        {
            var list = await _context.Purchases
                .Include(p => p.Supplier)
                .Include(p => p.Staff)
                .ToListAsync();
            return View(list);
        }

        // GET: Purchases/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var purchase = await _context.Purchases
                .Include(p => p.Supplier)
                .Include(p => p.Staff)
                .Include(p => p.PurchaseDetails)
                  .ThenInclude(d => d.Medicine)
                .FirstOrDefaultAsync(p => p.PurchaseId == id);
            if (purchase == null) return NotFound();
            return View(purchase);
        }

        // GET: Purchases/Create
        public IActionResult Create()
        {
            PopulateDropdowns();
            return View(new Purchase { PurchaseDate = DateTime.Now });
        }

        // POST: Purchases/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Purchase purchase)
        {
            PopulateDropdowns();
            if (!ModelState.IsValid) return View(purchase);

            purchase.CreatedAt = DateTime.Now;
            _context.Add(purchase);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Purchases/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var purchase = await _context.Purchases.FindAsync(id);
            if (purchase == null) return NotFound();

            PopulateDropdowns();
            return View(purchase);
        }

        // POST: Purchases/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Purchase purchase)
        {
            if (id != purchase.PurchaseId) return NotFound();
            PopulateDropdowns();
            if (!ModelState.IsValid) return View(purchase);

            try
            {
                purchase.UpdatedAt = DateTime.Now;
                _context.Update(purchase);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Purchases.Any(e => e.PurchaseId == id)) return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Purchases/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var purchase = await _context.Purchases
                .Include(p => p.Supplier)
                .Include(p => p.Staff)
                .FirstOrDefaultAsync(p => p.PurchaseId == id);
            if (purchase == null) return NotFound();
            return View(purchase);
        }

        // POST: Purchases/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var purchase = await _context.Purchases.FindAsync(id);
            if (purchase != null)
            {
                _context.Purchases.Remove(purchase);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
