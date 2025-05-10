using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MedicineStore.Data;
using MedicineStore.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using MustafaviManagementApp.Models;

namespace MedicineStore.Controllers
{
    public class InventorysController : Controller
    {
        private readonly AppDbContext _db;
        public InventorysController(AppDbContext context) => _db = context;

        // Populate medicine dropdown
        private void PopulateMedicines()
        {
            ViewBag.Medicines = new SelectList(
                _db.Medicines
                        .OrderBy(m => m.MedicineName)
                        .ToList(),
                "MedicineId",
                "MedicineName"
            );
        }

        // GET: Inventories
        public async Task<IActionResult> Index()
        {
            var items = await _db.Inventorys
                .Include(i => i.Medicine)
                .ToListAsync();
            return View(items);
        }

        // GET: Inventories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var inventory = await _db.Inventorys
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
            _db.Add(inventory);

            /* 🔸 Ledger: IN */
            await LedgerEntry(
                inventory.MedicineId,
                +inventory.QuantityOnHand,     // پوزیٹیو
                "IN"
            );

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Inventories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var inventory = await _db.Inventorys.FindAsync(id);
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
                // +ve ⇒ stock increase, -ve ⇒ decrease
                var dbInv = await _db.Inventorys
                                  .AsNoTracking()
                                  .FirstAsync(i => i.InventoryId == id);
                int delta = inventory.QuantityOnHand - dbInv.QuantityOnHand;
                // +ve ⇒ stock increase, -ve ⇒ decrease


                inventory.UpdatedAt = DateTime.Now;
                _db.Update(inventory);

                //----- Leader -----
                if (delta != 0)
                {
                    string action = delta > 0 ? "ADJUST_IN" : "ADJUST_OUT";
                    await LedgerEntry(
                        inventory.MedicineId,
                        delta,
                        action
                    );
                }

                await _db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_db.Inventorys.Any(e => e.InventoryId == id))
                    return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Inventories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var inventory = await _db.Inventorys
                .Include(i => i.Medicine)
                .FirstOrDefaultAsync(i => i.InventoryId == id);
            if (inventory == null) return NotFound();
            return View(inventory);
        }

        // POST: Inventories/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var inventory = await _db.Inventorys.FindAsync(id);
            if (inventory != null)
            {
                /* 🔸 Ledger: SCRAP_OUT */
                await LedgerEntry(
                    inventory.MedicineId,
                    -inventory.QuantityOnHand, 
                    "SCRAP_OUT"
                );
                /* 🔸 Ledger: SCRAP_OUT */

                _db.Inventorys.Remove(inventory);
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }



        private async Task LedgerEntry(int medicineId, int qtyDelta, string action, int? saleId = null, int? purchaseId = null)
        {
            var inventory = await _db.Inventorys
                                     .Where(i => i.MedicineId == medicineId)
                                     .Select(i => new { i.QuantityOnHand, i.ReservedQty })
                                     .FirstOrDefaultAsync();

            int qtyBeforeChange = 0; // Default to 0 if no inventory record exists

            if (inventory != null)
            {
                qtyBeforeChange = inventory.QuantityOnHand - inventory.ReservedQty;
            }

            var entry = new StockLedger
            {
                MedicineId = medicineId,
                SaleId = saleId,
                PurchaseId = purchaseId,
                ActionType = action,
                QtyChange = qtyDelta,
                QtyBeforeChange = qtyBeforeChange,
                BalanceAfter = qtyBeforeChange + qtyDelta,
                CreatedAt = DateTime.Now
            };

            _db.StockLedgers.Add(entry);
        }



    }
}
