using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedicineStore.Data;
using MedicineStore.Models;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MedicineStore.Controllers
{
    public class PurchaseDetailsController : Controller
    {
        private readonly AppDbContext _context;
        public PurchaseDetailsController(AppDbContext context) => _context = context;

        // Populate purchases and medicines dropdowns
        private void PopulateDropdowns()
        {
            ViewBag.Purchases = new SelectList(
                _context.Purchases.OrderBy(p => p.PurchaseDate).ToList(),
                "PurchaseId", "PurchaseId"
            );
            ViewBag.Medicines = new SelectList(
                _context.Medicines.OrderBy(m => m.MedicineName).ToList(),
                "MedicineId", "MedicineName"
            );
        }

        // GET: PurchaseDetails
        public async Task<IActionResult> Index()
        {
            var details = await _context.PurchaseDetails
                .Include(d => d.Purchase)
                .Include(d => d.Medicine)
                .ToListAsync();
            return View(details);
        }

        // GET: PurchaseDetails/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var detail = await _context.PurchaseDetails
                .Include(d => d.Purchase)
                .Include(d => d.Medicine)   
                .FirstOrDefaultAsync(d => d.PurchaseDetailId == id);
            if (detail == null) return NotFound();
            return View(detail);
        }

        // GET: PurchaseDetails/Create
        public IActionResult Create()
        {
            PopulateDropdowns();
            return View(new PurchaseDetail());
        }

        // POST: PurchaseDetails/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PurchaseDetail detail)
        {
            PopulateDropdowns();
            if (!ModelState.IsValid) return View(detail);

            _context.Add(detail);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: PurchaseDetails/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var detail = await _context.PurchaseDetails.FindAsync(id);
            if (detail == null) return NotFound();

            PopulateDropdowns();
            return View(detail);
        }

        // POST: PurchaseDetails/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PurchaseDetail detail)
        {
            if (id != detail.PurchaseDetailId) return NotFound();
            PopulateDropdowns();
            if (!ModelState.IsValid) return View(detail);

            try
            {
                _context.Update(detail);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.PurchaseDetails.Any(e => e.PurchaseDetailId == id)) return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: PurchaseDetails/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var detail = await _context.PurchaseDetails
                .Include(d => d.Purchase)
                .Include(d => d.Medicine)
                .FirstOrDefaultAsync(d => d.PurchaseDetailId == id);
            if (detail == null) return NotFound();
            return View(detail);
        }

        // POST: PurchaseDetails/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var detail = await _context.PurchaseDetails.FindAsync(id);
            if (detail != null)
            {
                _context.PurchaseDetails.Remove(detail);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}