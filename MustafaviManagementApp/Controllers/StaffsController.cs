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
    public class StaffsController : Controller
    {
        private readonly AppDbContext _context;
        public StaffsController(AppDbContext context) => _context = context;

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

        // GET: Staffs
        public async Task<IActionResult> Index()
        {
            var staffs = await _context.Staffs
                .Include(s => s.Store)
                .ToListAsync();
            return View(staffs);
        }

        // GET: Staffs/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var staff = await _context.Staffs
                .Include(s => s.Store)
                .FirstOrDefaultAsync(s => s.StaffId == id);
            if (staff == null) return NotFound();
            return View(staff);
        }

        // GET: Staffs/Create
        public IActionResult Create()
        {
            PopulateStores();
            return View(new Staff());
        }

        // POST: Staffs/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Staff staff)
        {
            PopulateStores();
            if (!ModelState.IsValid)
                return View(staff);

            staff.CreatedAt = DateTime.Now;
            _context.Add(staff);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Staffs/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var staff = await _context.Staffs.FindAsync(id);
            if (staff == null) return NotFound();

            PopulateStores();
            return View(staff);
        }

        // POST: Staffs/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Staff staff)
        {
            if (id != staff.StaffId) return NotFound();

            PopulateStores();
            if (!ModelState.IsValid)
                return View(staff);

            try
            {
                staff.UpdatedAt = DateTime.Now;
                _context.Update(staff);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Staffs.Any(e => e.StaffId == id))
                    return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Staffs/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var staff = await _context.Staffs
                .Include(s => s.Store)
                .FirstOrDefaultAsync(s => s.StaffId == id);
            if (staff == null) return NotFound();
            return View(staff);
        }

        // POST: Staffs/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var staff = await _context.Staffs.FindAsync(id);
            if (staff != null)
            {
                _context.Staffs.Remove(staff);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
