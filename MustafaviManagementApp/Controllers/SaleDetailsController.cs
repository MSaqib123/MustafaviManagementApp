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
    public class SaleDetailsController : Controller
    {
        private readonly AppDbContext _context;
        public SaleDetailsController(AppDbContext context) => _context = context;

        // Populate dropdowns for Sale and Medicine
        private void PopulateDropdowns()
        {
            ViewBag.Sales = new SelectList(
                _context.Sales.OrderBy(s => s.SaleDate).ToList(),
                "SaleId", "SaleId"
            );
            ViewBag.Medicines = new SelectList(
                _context.Medicines.OrderBy(m => m.MedicineName).ToList(),
                "MedicineId", "MedicineName"
            );
        }

        // GET: SaleDetails
        public async Task<IActionResult> Index()
        {
            var details = await _context.SaleDetails
                .Include(d => d.Sale)
                .Include(d => d.Medicine)
                .ToListAsync();
            return View(details);
        }

        // GET: SaleDetails/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var detail = await _context.SaleDetails
                .Include(d => d.Sale)
                .Include(d => d.Medicine)
                .FirstOrDefaultAsync(d => d.SaleDetailId == id);
            if (detail == null) return NotFound();
            return View(detail);
        }

        // GET: SaleDetails/Create
        public IActionResult Create()
        {
            PopulateDropdowns();
            return View(new SaleDetail());
        }

        // POST: SaleDetails/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SaleDetail detail)
        {
            PopulateDropdowns();
            if (!ModelState.IsValid)
                return View(detail);

            _context.Add(detail);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: SaleDetails/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var detail = await _context.SaleDetails.FindAsync(id);
            if (detail == null) return NotFound();

            PopulateDropdowns();
            return View(detail);
        }

        // POST: SaleDetails/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SaleDetail detail)
        {
            if (id != detail.SaleDetailId) return NotFound();

            PopulateDropdowns();
            if (!ModelState.IsValid)
                return View(detail);

            try
            {
                _context.Update(detail);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.SaleDetails.Any(e => e.SaleDetailId == id))
                    return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: SaleDetails/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var detail = await _context.SaleDetails
                .Include(d => d.Sale)
                .Include(d => d.Medicine)
                .FirstOrDefaultAsync(d => d.SaleDetailId == id);
            if (detail == null) return NotFound();
            return View(detail);
        }

        // POST: SaleDetails/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var detail = await _context.SaleDetails.FindAsync(id);
            if (detail != null)
            {
                _context.SaleDetails.Remove(detail);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
