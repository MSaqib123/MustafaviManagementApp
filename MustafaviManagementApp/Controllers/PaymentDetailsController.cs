using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MedicineStore.Data;
using MedicineStore.Models;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MedicineStore.Controllers
{
    public class PaymentDetailsController : Controller
    {
        private readonly AppDbContext _context;
        public PaymentDetailsController(AppDbContext context) => _context = context;

        // Populate Payments dropdown
        private void PopulatePayments()
        {
            ViewBag.Payments = new SelectList(
                _context.Payments
                        .OrderBy(p => p.PaymentDate)
                        .ToList(),
                "PaymentId",
                "PaymentId"
            );
        }

        // GET: PaymentDetails
        public async Task<IActionResult> Index()
        {
            var details = await _context.PaymentDetails
                .Include(d => d.Payment)
                .ToListAsync();
            return View(details);
        }

        // GET: PaymentDetails/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var detail = await _context.PaymentDetails
                .Include(d => d.Payment)
                .FirstOrDefaultAsync(d => d.PaymentDetailId == id);

            if (detail == null) return NotFound();
            return View(detail);
        }

        // GET: PaymentDetails/Create
        public IActionResult Create()
        {
            PopulatePayments();
            return View(new PaymentDetail { PaidAt = DateTime.Now });
        }

        // POST: PaymentDetails/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PaymentDetail detail)
        {
            PopulatePayments();
            if (!ModelState.IsValid)
                return View(detail);

            detail.CreatedAt = DateTime.Now;
            _context.Add(detail);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: PaymentDetails/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var detail = await _context.PaymentDetails.FindAsync(id);
            if (detail == null) return NotFound();

            PopulatePayments();
            return View(detail);
        }

        // POST: PaymentDetails/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PaymentDetail detail)
        {
            if (id != detail.PaymentDetailId) return NotFound();
            PopulatePayments();
            if (!ModelState.IsValid)
                return View(detail);

            try
            {
                detail.UpdatedAt = DateTime.Now;
                _context.Update(detail);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.PaymentDetails.Any(e => e.PaymentDetailId == id))
                    return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: PaymentDetails/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var detail = await _context.PaymentDetails
                .Include(d => d.Payment)
                .FirstOrDefaultAsync(d => d.PaymentDetailId == id);

            if (detail == null) return NotFound();
            return View(detail);
        }

        // POST: PaymentDetails/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var detail = await _context.PaymentDetails.FindAsync(id);
            if (detail != null)
            {
                _context.PaymentDetails.Remove(detail);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}