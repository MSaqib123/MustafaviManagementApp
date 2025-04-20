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
    public class PrescriptionsController : Controller
    {
        private readonly AppDbContext _context;
        public PrescriptionsController(AppDbContext context) => _context = context;

        // Populate the Sale dropdown
        private void PopulateSales()
        {
            ViewBag.Sales = new SelectList(
                _context.Sales
                        .OrderBy(s => s.SaleDate)
                        .ToList(),
                "SaleId",
                "SaleId"
            );
        }

        // GET: Prescriptions
        public async Task<IActionResult> Index()
        {
            var list = await _context.Prescriptions
                                     .Include(p => p.Sale)
                                     .ToListAsync();
            return View(list);
        }

        // GET: Prescriptions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var prescription = await _context.Prescriptions
                .Include(p => p.Sale)
                .FirstOrDefaultAsync(p => p.PrescriptionId == id);

            if (prescription == null) return NotFound();
            return View(prescription);
        }

        // GET: Prescriptions/Create
        public IActionResult Create()
        {
            PopulateSales();
            return View(new Prescription { PrescriptionDate = DateTime.Now });
        }

        // POST: Prescriptions/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Prescription prescription)
        {
            PopulateSales();
            if (!ModelState.IsValid)
                return View(prescription);

            prescription.CreatedAt = DateTime.Now;
            _context.Add(prescription);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Prescriptions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var prescription = await _context.Prescriptions.FindAsync(id);
            if (prescription == null) return NotFound();

            PopulateSales();
            return View(prescription);
        }

        // POST: Prescriptions/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Prescription prescription)
        {
            if (id != prescription.PrescriptionId) return NotFound();

            PopulateSales();
            if (!ModelState.IsValid)
                return View(prescription);

            try
            {
                prescription.UpdatedAt = DateTime.Now;
                _context.Update(prescription);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Prescriptions.Any(e => e.PrescriptionId == id))
                    return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Prescriptions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var prescription = await _context.Prescriptions
                .Include(p => p.Sale)
                .FirstOrDefaultAsync(p => p.PrescriptionId == id);

            if (prescription == null) return NotFound();
            return View(prescription);
        }

        // POST: Prescriptions/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var prescription = await _context.Prescriptions.FindAsync(id);
            if (prescription != null)
            {
                _context.Prescriptions.Remove(prescription);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
