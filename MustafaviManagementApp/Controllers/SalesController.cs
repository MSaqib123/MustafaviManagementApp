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
    public class SalesController : Controller
    {
        private readonly AppDbContext _context;

        public SalesController(AppDbContext context)
            => _context = context;

        // Helper to populate dropdowns
        private void PopulateDropdowns()
        {
            ViewBag.Customers = new SelectList(
                _context.Customers
                        .OrderBy(c => c.CustomerName)
                        .ToList(),
                "CustomerId", "CustomerName"
            );
            ViewBag.Staffs = new SelectList(
                _context.Staffs
                        .OrderBy(s => s.StaffName)
                        .ToList(),
                "StaffId", "StaffName"
            );
        }

        // GET: Sales
        public async Task<IActionResult> Index()
        {
            var sales = await _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.Staff)
                .ToListAsync();
            return View(sales);
        }

        // GET: Sales/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var sale = await _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.Staff)
                .Include(s => s.SaleDetails)
                  .ThenInclude(d => d.Medicine)
                .Include(s => s.Prescriptions)
                .FirstOrDefaultAsync(m => m.SaleId == id);

            if (sale == null) return NotFound();
            return View(sale);
        }

        // GET: Sales/Create
        public IActionResult Create()
        {
            PopulateDropdowns();
            return View(new Sale { SaleDate = DateTime.Now });
        }

        // POST: Sales/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Sale sale)
        {
            PopulateDropdowns();
            if (!ModelState.IsValid)
                return View(sale);

            sale.CreatedAt = DateTime.Now;
            _context.Add(sale);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Sales/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var sale = await _context.Sales.FindAsync(id);
            if (sale == null) return NotFound();

            PopulateDropdowns();
            return View(sale);
        }

        // POST: Sales/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Sale sale)
        {
            if (id != sale.SaleId) return NotFound();

            PopulateDropdowns();
            if (!ModelState.IsValid)
                return View(sale);

            try
            {
                sale.UpdatedAt = DateTime.Now;
                _context.Update(sale);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Sales.Any(e => e.SaleId == id))
                    return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Sales/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var sale = await _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.Staff)
                .FirstOrDefaultAsync(m => m.SaleId == id);

            if (sale == null) return NotFound();
            return View(sale);
        }

        // POST: Sales/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var sale = await _context.Sales.FindAsync(id);
            if (sale != null)
            {
                _context.Sales.Remove(sale);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
