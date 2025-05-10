using MedicineStore.Data;
using MedicineStore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MustafaviManagementApp.Models;
using MustafaviManagementApp.ViewModels;
using Newtonsoft.Json;

namespace MustafaviManagementApp.Controllers
{
    public class POSController : Controller
    {
        private readonly AppDbContext _db;
        public POSController(AppDbContext db) => _db = db;

        // ─────────── INDEX ───────────
        public async Task<IActionResult> Index()
        {
            await LoadDropdowns();
            return View(new POSViewModel { StaffId = 1 });
        }

        // ─── PAY / HOLD / RECALL ───
        [HttpPost]
        public async Task<IActionResult> Index(string itemsJson, POSViewModel vm, string actionType, int? saleId)
        {
            await LoadDropdowns();

            // 1) rehydrate
            if (!string.IsNullOrWhiteSpace(itemsJson))
                vm.Items = JsonConvert.DeserializeObject<List<POSLineItem>>(itemsJson)!;

            if (!vm.Items.Any())
            {
                ModelState.AddModelError("", "Cart is empty");
                return View(vm);
            }

            // 2) if paying a previously held order ⇒ release & delete
            if (actionType == "Pay" && saleId.HasValue)
            {
                var held = await _db.Sales
                    .Include(s => s.SaleDetails)
                    .FirstOrDefaultAsync(s => s.SaleId == saleId && s.IsHeld);
                if (held != null)
                {
                    foreach (var d in held.SaleDetails)
                    {
                        var inv = await _db.Inventorys.FirstAsync(i => i.MedicineId == d.MedicineId);
                        inv.ReservedQty -= d.Quantity;   // un-reserve
                        inv.QuantityOnHand -= d.Quantity; // ship it
                    }
                    _db.SaleDetails.RemoveRange(held.SaleDetails);
                    _db.Sales.Remove(held);
                    await _db.SaveChangesAsync();
                }
            }

            // 3) create new Sale header
            var sale = new Sale
            {
                CustomerId                  = (vm.CustomerId==null) ? 1 : null,
                StaffId                     = vm.StaffId == 0 ? 1 : vm.StaffId,
                SaleDate                    = DateTime.Now,
                TotalAmountBeforDiscount    = vm.Gross,            // ← new
                TotalAmountBeforVAT         = vm.Gross,              // ← new
                Discount                    = vm.DiscountValue,
                TotalAmount                 = vm.GrandTotal,
                PaymentMethod               = vm.PaymentMethod,
                PaymentStatus               = actionType == "Hold" ? "Pending" : "Paid",
                IsHeld                      = actionType == "Hold",
                CreatedAt                   = DateTime.Now
            };
            _db.Sales.Add(sale);
            await _db.SaveChangesAsync();

            // 4) line-items
            foreach (var l in vm.Items)
            {
                var inv = await _db.Inventorys.FirstAsync(i => i.MedicineId == l.MedicineId);

                if (actionType == "Hold")
                {
                    if (inv.QuantityOnHand - inv.ReservedQty < l.Quantity)
                    {
                        ModelState.AddModelError("", $"{l.MedicineName} out of stock");
                        return View(vm);
                    }
                    inv.ReservedQty += l.Quantity;

                    //------------- Leader ---------
                    // 🔸 Ledger: RESERVE
                    await LedgerEntry(l.MedicineId, -l.Quantity, "RESERVE", saleId: sale.SaleId);
                }
                else // Pay
                {
                    if (inv.QuantityOnHand - inv.ReservedQty < l.Quantity)
                    {
                        ModelState.AddModelError("", $"{l.MedicineName} out of stock");
                        return View(vm);
                    }
                    inv.QuantityOnHand -= l.Quantity;

                    //------------- Leader ---------
                    // 🔸 Ledger: OUT
                    //await LedgerEntry(l.MedicineId, -l.Quantity, "OUT", saleId: sale.SaleId);
                }

                _db.SaleDetails.Add(new SaleDetail
                {
                    SaleId     = sale.SaleId,
                    MedicineId = l.MedicineId,
                    Quantity   = l.Quantity,
                    UnitPrice  = l.UnitPrice,
                    Discount   = l.Discount,
                    SubTotal   = l.Quantity * l.UnitPrice - l.Discount   // ← new
                });
            }

            // 5) real payment? (only on Pay)
            if (actionType == "Pay")
            {
                _db.Payments.Add(new Payment
                {
                    ReferenceType = "Sale",
                    ReferenceId   = sale.SaleId,
                    TotalAmount   = sale.TotalAmount,
                    Status        = "Completed",
                    PaymentDate   = DateTime.Now,
                    CreatedAt     = DateTime.Now
                });

                //------------- Leader ---------
                // Ledger OUT
                foreach (var itm in vm.Items)
                {
                    await LedgerEntry(itm.MedicineId, -itm.Quantity, "OUT", saleId: sale.SaleId);
                }
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ─── UPDATE HELD ───
        [HttpPost]
        public async Task<IActionResult> UpdateHold(
                int saleId, string itemsJson, POSViewModel vm)
        {
            if (!string.IsNullOrEmpty(itemsJson))
                vm.Items = JsonConvert.DeserializeObject<List<POSLineItem>>(itemsJson)!;

            var sale = await _db.Sales
                .Include(s => s.SaleDetails)
                .FirstOrDefaultAsync(s => s.SaleId == saleId && s.IsHeld);
            if (sale == null) return NotFound();

            /* ─ 1️⃣ پُرانی لائنیں واپس (RELEASE) ─ */
            foreach (var old in sale.SaleDetails)
            {
                var inv = await _db.Inventorys
                                   .FirstAsync(i => i.MedicineId == old.MedicineId);

                inv.ReservedQty -= old.Quantity;                         // انوینٹری
                await LedgerEntry(old.MedicineId, +old.Quantity,          // لیجر
                                  "RELEASE", saleId: sale.SaleId);
            }
            _db.SaleDetails.RemoveRange(sale.SaleDetails);
            await _db.SaveChangesAsync();

            /* ─ 2️⃣ نئی لائنیں دوبارہ ریزرو (RESERVE) ─ */
            foreach (var l in vm.Items)
            {
                var inv = await _db.Inventorys
                                   .FirstAsync(i => i.MedicineId == l.MedicineId);

                if (inv.QuantityOnHand - inv.ReservedQty < l.Quantity)
                {
                    ModelState.AddModelError("", $"{l.MedicineName} out of stock");
                    return View("Index", vm);
                }

                inv.ReservedQty += l.Quantity;                           // انوینٹری
                await LedgerEntry(l.MedicineId, -l.Quantity,              // لیجر
                                  "RESERVE", saleId: sale.SaleId);

                _db.SaleDetails.Add(new SaleDetail
                {
                    SaleId = sale.SaleId,
                    MedicineId = l.MedicineId,
                    Quantity = l.Quantity,
                    UnitPrice = l.UnitPrice,
                    Discount = l.Discount,
                    SubTotal = l.Quantity * l.UnitPrice - l.Discount
                });
            }

            /* ─ 3️⃣ ہیڈر اپ ڈیٹ ─ */
            sale.TotalAmountBeforDiscount = vm.Gross;
            sale.TotalAmountBeforVAT = vm.Gross;
            sale.Discount = vm.DiscountValue;
            sale.TotalAmount = vm.GrandTotal;
            sale.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        // ─── DELETE HELD ───
        [HttpPost]
        public async Task<IActionResult> DeleteHold(int id)
        {
            await using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                var sale = await _db.Sales
                    .Include(s => s.SaleDetails)
                    .FirstOrDefaultAsync(s => s.SaleId == id && s.IsHeld);

                if (sale == null) return NotFound();

                /* 1️⃣  reverse reserved stock + new RELEASE rows */
                foreach (var d in sale.SaleDetails)
                {
                    var inv = await _db.Inventorys
                                       .FirstAsync(i => i.MedicineId == d.MedicineId);

                    inv.ReservedQty -= d.Quantity;

                    await LedgerEntry(d.MedicineId, +d.Quantity, "RELEASE", saleId: null);
                }

                /* 2️⃣  detach old RESERVE ledger rows */
                var oldLedgers = await _db.StockLedgers
                    .Where(l => l.SaleId == sale.SaleId)
                    .ToListAsync();

                foreach (var l in oldLedgers)
                    l.SaleId = null;                           // ← break FK

                /* 3️⃣  delete sale + its details */
                _db.SaleDetails.RemoveRange(sale.SaleDetails);
                _db.Sales.Remove(sale);

                await _db.SaveChangesAsync();
                await tx.CommitAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return Problem(ex.InnerException?.Message ?? ex.Message);
            }
        }




        // ─── JSON FOR UI ───
        [HttpGet]
        public async Task<IActionResult> GetHeldOrders() =>
            Json(await _db.Sales
                .Where(s => s.IsHeld)
                .Select(s => new {
                    saleId      = s.SaleId,
                    totalAmount = s.TotalAmount,
                    totalQty    = s.SaleDetails.Sum(d => d.Quantity)
                })
                .OrderByDescending(x => x.saleId)
                .ToListAsync());

        [HttpGet]
        public async Task<IActionResult> GetHeldOrder(int id)
        {
            var s = await _db.Sales
                .Include(x => x.SaleDetails).ThenInclude(d => d.Medicine)
                .FirstOrDefaultAsync(x => x.SaleId == id && x.IsHeld);
            if (s == null) return NotFound();

            return Json(new
            {
                overallDiscount    = s.Discount,
                discountIsPercent  = true,
                vatPercent         = 0,
                items = s.SaleDetails.Select(d => new {
                    medicineId   = d.MedicineId,
                    medicineName = d.Medicine.MedicineName,
                    quantity     = d.Quantity,
                    unitPrice    = d.UnitPrice,
                    discount     = d.Discount,
                    subTotal     = d.SubTotal
                })
            });
        }

        // ─── LOAD DROPDOWNS ───
        private async Task LoadDropdowns()
        {
            ViewBag.MedicinesLite = await _db.Medicines
                .Select(m => new {
                  m.MedicineId,
                  m.MedicineName,
                  m.CategoryId,
                  UnitPrice = (m.SingleUnitPrice ?? m.CotanUnitPrice) ?? 0m, // ← never null
                  m.Image,
                  m.UrduName,
                })
                .ToListAsync();

            // Available = on‐hand minus reserved
            ViewBag.InventoryLite = await _db.Inventorys
                .Select(i => new {
                  i.MedicineId,
                  QuantityOnHand = i.QuantityOnHand - i.ReservedQty
                })
                .ToListAsync();

            ViewBag.CategoriesLite = await _db.Categorys
                .Select(c => new { c.CategoryId, c.CategoryName })
                .ToListAsync();

            ViewBag.Categories = await _db.Categorys.ToListAsync();
            ViewBag.Medicines  = await _db.Medicines.ToListAsync();
            ViewBag.Customers  = await _db.Customers.ToListAsync();
            ViewBag.Staffs     = await _db.Staffs.ToListAsync();
        }


        private async Task LedgerEntry(
        int medicineId, int qtyDelta, string action,
        int? saleId = null, int? purchaseId = null)
        {
            // 1) پچھلا بیلنس
            var balance = await _db.Inventorys
                            .Where(i => i.MedicineId == medicineId)
                            .Select(i => i.QuantityOnHand - i.ReservedQty)
                            .FirstAsync();

            // 2) لیجر میں لکھیں
            var entry = new StockLedger
            {
                MedicineId = medicineId,
                SaleId = saleId,
                PurchaseId = purchaseId,
                ActionType = action,          // IN / OUT / RESERVE / RELEASE
                QtyChange = qtyDelta,        // +/-
                QtyBeforeChange = balance,
                BalanceAfter = balance + qtyDelta,
                CreatedAt = DateTime.Now
            };
            _db.StockLedgers.Add(entry);

            //// 3) انوینٹری ٹیبل بھی ساتھ ساتھ بدلے (اگر آپ نے OnHand رکھنا ہے)
            //var inv = await _db.Inventorys.FirstAsync(i => i.MedicineId == medicineId);
            //switch (action)
            //{
            //    case "IN": inv.QuantityOnHand += qtyDelta; break;   // +ve
            //    case "OUT": inv.QuantityOnHand -= -qtyDelta; break;   // qtyDelta = -ve
            //    case "RESERVE": inv.ReservedQty += -qtyDelta; break;   // qtyDelta = -ve
            //    case "RELEASE": inv.ReservedQty -= qtyDelta; break;   // +ve
            //}
        }

    }
}

//===================================================================
//========================= Version # 2 =============================
//===================================================================
#region Version # 2
//using MedicineStore.Data;
//using MedicineStore.Models;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using MustafaviManagementApp.ViewModels;
//using Newtonsoft.Json;

//namespace MustafaviManagementApp.Controllers
//{
//    public class POSController : Controller
//    {
//        private readonly AppDbContext _db;
//        public POSController(AppDbContext db) => _db = db;

//        public async Task<IActionResult> Index()
//        {
//            await LoadDropdowns();
//            return View(new POSViewModel { StaffId = 1 });
//        }

//        /* ---------- POST (Pay or Hold) ---------- */
//        [HttpPost]
//        public async Task<IActionResult> Index(
//            string itemsJson,
//            POSViewModel vm,
//            string actionType,
//            int? saleId
//        )
//        {
//            // Reload your dropdowns as usual
//            await LoadDropdowns();

//            // Rehydrate the cart
//            if (!string.IsNullOrWhiteSpace(itemsJson))
//                vm.Items = JsonConvert.DeserializeObject<List<POSLineItem>>(itemsJson)!;

//            // Validation
//            if (!vm.Items.Any())
//            {
//                ModelState.AddModelError("", "Cart is empty.");
//                return View(vm);
//            }
//            if (!ModelState.IsValid)
//                return View(vm);

//            // 2) If we're paying a previously held order, delete it now
//            if (actionType == "Pay" && saleId.HasValue)
//            {
//                var held = await _db.Sales
//                    .Include(s => s.SaleDetails)
//                    .FirstOrDefaultAsync(s => s.SaleId == saleId.Value && s.IsHeld);

//                if (held != null)
//                {
//                    // remove its details and then the sale header
//                    _db.SaleDetails.RemoveRange(held.SaleDetails);
//                    _db.Sales.Remove(held);
//                    await _db.SaveChangesAsync();
//                }
//            }

//            // 3) create the new sale header (Paid or Held)
//            var sale = new Sale
//            {
//                CustomerId = vm.CustomerId,
//                StaffId = (vm.StaffId == 0 ? 1 : vm.StaffId),
//                SaleDate = DateTime.Now,
//                TotalAmount = vm.GrandTotal,
//                Discount = vm.DiscountValue,
//                PaymentMethod = vm.PaymentMethod,
//                PaymentStatus = actionType == "Hold" ? "Pending" : "Paid",
//                IsHeld = actionType == "Hold",
//                CreatedAt = DateTime.Now
//            };
//            _db.Sales.Add(sale);
//            await _db.SaveChangesAsync();

//            // 4) line‐items / inventory
//            foreach (var line in vm.Items)
//            {
//                if (actionType != "Hold")
//                {
//                    var inv = await _db.Inventorys.FirstOrDefaultAsync(i => i.MedicineId == line.MedicineId);
//                    if (inv == null || inv.QuantityOnHand < line.Quantity)
//                    {
//                        ModelState.AddModelError("", $"{line.MedicineName} out of stock.");
//                        return View(vm);
//                    }
//                    inv.QuantityOnHand -= line.Quantity;
//                }

//                _db.SaleDetails.Add(new SaleDetail
//                {
//                    SaleId = sale.SaleId,
//                    MedicineId = line.MedicineId,
//                    Quantity = line.Quantity,
//                    UnitPrice = line.UnitPrice,
//                    Discount = line.Discount
//                });
//            }

//            // 5) create a real payment if it’s Pay
//            if (actionType == "Pay")
//            {
//                _db.Payments.Add(new Payment
//                {
//                    ReferenceType = "Sale",
//                    ReferenceId = sale.SaleId,
//                    TotalAmount = sale.TotalAmount,
//                    Status = "Completed",
//                    PaymentDate = DateTime.Now,
//                    CreatedAt = DateTime.Now
//                });
//            }

//            await _db.SaveChangesAsync();

//            // 6) redirect back to a fresh POS
//            return RedirectToAction(nameof(Index));
//        }

//        //[HttpPost]
//        //public async Task<IActionResult> Index(string itemsJson, POSViewModel vm, string actionType)
//        //{
//        //    try
//        //    {
//        //        if (!string.IsNullOrWhiteSpace(itemsJson))
//        //            vm.Items = JsonConvert.DeserializeObject<List<POSLineItem>>(itemsJson)!;

//        //        await LoadDropdowns();

//        //        if (!vm.Items.Any())
//        //        {
//        //            ModelState.AddModelError("", "Cart is empty."); return View(vm);
//        //        }
//        //        if (!ModelState.IsValid) return View(vm);

//        //        // 1️⃣ create Sale header
//        //        var sale = new Sale
//        //        {
//        //            CustomerId = vm.CustomerId,
//        //            StaffId = (vm.StaffId == 0 ? 1 : vm.StaffId),
//        //            SaleDate = DateTime.Now,
//        //            TotalAmount = vm.GrandTotal,
//        //            Discount = vm.DiscountValue,
//        //            PaymentMethod = vm.PaymentMethod,
//        //            PaymentStatus = actionType == "Hold" ? "Pending" : "Paid",
//        //            IsHeld = actionType == "Hold",
//        //            CreatedAt = DateTime.Now
//        //        };
//        //        _db.Sales.Add(sale);
//        //        await _db.SaveChangesAsync();

//        //        // 2️⃣ line-items (NO inventory deduction for Hold)
//        //        foreach (var line in vm.Items)
//        //        {
//        //            if (actionType != "Hold")
//        //            {
//        //                var inv = await _db.Inventorys.FirstOrDefaultAsync(i => i.MedicineId == line.MedicineId);
//        //                if (inv == null || inv.QuantityOnHand < line.Quantity)
//        //                {
//        //                    ModelState.AddModelError("", $"{line.MedicineName} out of stock.");
//        //                    return View(vm);
//        //                }
//        //                inv.QuantityOnHand -= line.Quantity;
//        //            }

//        //            _db.SaleDetails.Add(new SaleDetail
//        //            {
//        //                SaleId = sale.SaleId,
//        //                MedicineId = line.MedicineId,
//        //                Quantity = line.Quantity,
//        //                UnitPrice = line.UnitPrice,
//        //                Discount = line.Discount
//        //            });
//        //        }

//        //        // 3️⃣ only real payment records if it’s Pay
//        //        if (actionType == "Pay")
//        //        {
//        //            _db.Payments.Add(new Payment
//        //            {
//        //                ReferenceType = "Sale",
//        //                ReferenceId = sale.SaleId,
//        //                TotalAmount = sale.TotalAmount,
//        //                Status = "Completed",
//        //                PaymentDate = DateTime.Now,
//        //                CreatedAt = DateTime.Now
//        //            });
//        //        }

//        //        await _db.SaveChangesAsync();
//        //        //return actionType == "Hold"
//        //        //     ? RedirectToAction(nameof(Index))
//        //        //     : RedirectToAction(nameof(Receipt), new { id = sale.SaleId });

//        //        return RedirectToAction(nameof(Index));
//        //    }
//        //    catch (Exception ex)
//        //    {

//        //        throw;
//        //    }
//        //}


//        // POST /POS/UpdateHold — update an existing held sale
//        [HttpPost]
//        public async Task<IActionResult> UpdateHold(int saleId, string itemsJson, POSViewModel vm)
//        {
//            if (!string.IsNullOrEmpty(itemsJson))
//                vm.Items = JsonConvert.DeserializeObject<List<POSLineItem>>(itemsJson);

//            await LoadDropdowns();
//            var sale = await _db.Sales.Include(s => s.SaleDetails)
//                                      .FirstOrDefaultAsync(s => s.SaleId == saleId);
//            if (sale == null) return NotFound();

//            // restore inventory
//            //foreach (var old in sale.SaleDetails)
//            //{
//            //    var inv = await _db.Inventorys.FirstAsync(i => i.MedicineId == old.MedicineId);
//            //    inv.QuantityOnHand += old.Quantity;
//            //}
//            _db.SaleDetails.RemoveRange(sale.SaleDetails);
//            await _db.SaveChangesAsync();

//            // apply new items
//            foreach (var line in vm.Items)
//            {
//                //var inv = await _db.Inventorys.FirstAsync(i => i.MedicineId == line.MedicineId);
//                //if (inv.QuantityOnHand < line.Quantity)
//                //{
//                //    ModelState.AddModelError("", $"{line.MedicineName} now out of stock.");
//                //    return View("Index", vm);
//                //}
//                //inv.QuantityOnHand -= line.Quantity;
//                _db.SaleDetails.Add(new SaleDetail
//                {
//                    SaleId = sale.SaleId,
//                    MedicineId = line.MedicineId,
//                    Quantity = line.Quantity,
//                    UnitPrice = line.UnitPrice,
//                    Discount = line.Discount
//                });
//            }

//            // update header
//            sale.TotalAmount = vm.GrandTotal;
//            sale.Discount = vm.DiscountValue;
//            sale.PaymentStatus = vm.PaymentStatus;
//            sale.UpdatedAt = DateTime.Now;

//            if (vm.PaymentStatus == "Paid")
//            {
//                _db.Payments.Add(new Payment
//                {
//                    ReferenceType = "Sale",
//                    ReferenceId = sale.SaleId,
//                    TotalAmount = sale.TotalAmount,
//                    Status = "Completed",
//                    PaymentDate = DateTime.Now,
//                    CreatedAt = DateTime.Now
//                });
//            }

//            await _db.SaveChangesAsync();
//            return vm.PaymentStatus == "Paid"
//                ? RedirectToAction("Receipt", new { id = sale.SaleId })
//                : RedirectToAction("Index");
//        }

//        // GET /POS/Receipt/123
//        public async Task<IActionResult> Receipt(int id)
//        {
//            var sale = await _db.Sales
//                .Include(s => s.SaleDetails).ThenInclude(d => d.Medicine)
//                .Include(s => s.Customer)
//                .Include(s => s.Staff)
//                .FirstOrDefaultAsync(s => s.SaleId == id);

//            return View(sale);
//        }

//        private async Task LoadDropdowns()
//        {
//            ViewBag.MedicinesLite = await _db.Medicines
//                .Select(m => new {
//                    m.MedicineId,
//                    m.MedicineName,
//                    m.CategoryId,
//                    UnitPrice = m.SingleUnitPrice ?? m.CotanUnitPrice
//                })
//                .ToListAsync();
//            ViewBag.InventoryLite = await _db.Inventorys
//                .Select(i => new { i.MedicineId, i.QuantityOnHand })
//                .ToListAsync();
//            ViewBag.CategoriesLite = await _db.Categorys
//                .Select(c => new { c.CategoryId, c.CategoryName })
//                .ToListAsync();
//            ViewBag.Categories = await _db.Categorys.ToListAsync();
//            ViewBag.Medicines = await _db.Medicines.ToListAsync();
//            ViewBag.Customers = await _db.Customers.ToListAsync();
//            ViewBag.Staffs = await _db.Staffs.ToListAsync();
//        }




//        // ----------  lightweight JSON endpoints ----------
//        // /POS/GetHeldOrders    → small list
//        [HttpGet]
//        public async Task<IActionResult> GetHeldOrders() =>
//        Json(await _db.Sales
//             .Where(s => s.IsHeld)
//             .Select(s => new {
//                 saleId = s.SaleId,
//                 totalAmount = s.TotalAmount,
//                 totalQty = s.SaleDetails.Sum(d => d.Quantity)
//             })
//             .OrderByDescending(s => s.saleId)
//             .ToListAsync());

//        // /POS/GetHeldOrder/123 → full details
//        [HttpGet]
//        public async Task<IActionResult> GetHeldOrder(int id)
//        {
//            var s = await _db.Sales.Include(x => x.SaleDetails).ThenInclude(d => d.Medicine)
//                                   .FirstOrDefaultAsync(x => x.SaleId == id && x.IsHeld);
//            if (s == null) return NotFound();
//            return Json(new
//            {
//                overallDiscount = s.Discount,
//                discountIsPercent = true,
//                vatPercent = 0,
//                items = s.SaleDetails.Select(d => new {
//                    medicineId = d.MedicineId,
//                    medicineName = d.Medicine.MedicineName,
//                    quantity = d.Quantity,
//                    unitPrice = d.UnitPrice
//                })
//            });
//        }

//        //  /POS/DeleteHold/123   ->  optional: delete a held order instead of recalling
//        [HttpPost]
//        public async Task<IActionResult> DeleteHold(int id)
//        {
//            var sale = await _db.Sales.FirstOrDefaultAsync(s => s.SaleId == id && s.PaymentStatus == "Pending");
//            if (sale == null) return NotFound();
//            _db.Sales.Remove(sale);
//            await _db.SaveChangesAsync();
//            return Ok();
//        }

//    }
//}
#endregion

//===================================================================
//========================= Version # 1 =============================
//===================================================================
#region version #1 
//using MedicineStore.Data;
//using MedicineStore.Models;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using MustafaviManagementApp.ViewModels;

//namespace MustafaviManagementApp.Controllers
//{
//    public class POSController : Controller
//    {
//        private readonly AppDbContext _db;
//        public POSController(AppDbContext db) => _db = db;

//        /* ---------- GET: /POS ---------- */
//        public async Task<IActionResult> Index()
//        {
//            var vm = new POSViewModel
//            {
//                StaffId = 1 // TODO: map to logged‑in user
//            };
//            await LoadDropdowns();
//            return View(vm);
//        }

//        /* ---------- POST: /POS ---------- */
//        [HttpPost]
//        public async Task<IActionResult> Index(POSViewModel vm, string actionType)
//        {
//            await LoadDropdowns();
//            if (!vm.Items.Any())
//            {
//                ModelState.AddModelError("", "Cart is empty.");
//                return View(vm);
//            }

//            if (!ModelState.IsValid) return View(vm);

//            // create sale (Paid or Pending)
//            var sale = new Sale
//            {
//                CustomerId = vm.CustomerId,
//                StaffId = vm.StaffId,
//                SaleDate = DateTime.Now,
//                TotalAmount = vm.GrandTotal,
//                Discount = vm.DiscountValue,
//                PaymentMethod = vm.PaymentMethod,
//                PaymentStatus = actionType == "Hold" ? "Pending" : vm.PaymentStatus,
//                CreatedAt = DateTime.Now
//            };
//            _db.Sales.Add(sale);
//            await _db.SaveChangesAsync();

//            /* line‑items & inventory check */
//            foreach (var line in vm.Items)
//            {
//                var stock = await _db.Inventorys.FirstOrDefaultAsync(i => i.MedicineId == line.MedicineId);
//                if (stock == null || stock.QuantityOnHand < line.Quantity)
//                {
//                    ModelState.AddModelError("", $"{line.MedicineName} out of stock.");
//                    return View(vm);
//                }
//                stock.QuantityOnHand -= line.Quantity;

//                _db.SaleDetails.Add(new SaleDetail
//                {
//                    SaleId = sale.SaleId,
//                    MedicineId = line.MedicineId,
//                    BatchNumber = line.BatchNumber,
//                    ExpiryDate = line.ExpiryDate,
//                    Quantity = line.Quantity,
//                    UnitPrice = line.UnitPrice,
//                    Discount = line.Discount,
//                });
//            }
//            if (actionType != "Hold" && vm.PaymentStatus == "Paid")
//            {
//                _db.Payments.Add(new Payment
//                {
//                    ReferenceType = "Sale",
//                    ReferenceId = sale.SaleId,
//                    TotalAmount = sale.TotalAmount,
//                    Status = "Completed",
//                    PaymentDate = DateTime.Now,
//                    CreatedAt = DateTime.Now
//                });
//            }
//            await _db.SaveChangesAsync();
//            return RedirectToAction("Receipt", new { id = sale.SaleId });
//        }

//        /* ---------- Recall Hold Order ---------- */
//        public async Task<IActionResult> Recall(int id)
//        {
//            var sale = await _db.Sales
//                .Include(s => s.SaleDetails)
//                  .ThenInclude(d => d.Medicine)
//                .FirstOrDefaultAsync(s => s.SaleId == id && s.PaymentStatus == "Pending");
//            if (sale == null) return NotFound();

//            var vm = new POSViewModel
//            {
//                CustomerId = sale.CustomerId,
//                StaffId = sale.StaffId,
//                PaymentStatus = "Pending",
//                Items = sale.SaleDetails.Select(d => new POSLineItem(
//                    d.MedicineId, d.Medicine.MedicineName, d.Quantity, d.UnitPrice, d.Discount, d.BatchNumber, d.ExpiryDate)).ToList()
//            };
//            await LoadDropdowns();
//            ViewBag.RecallSaleId = sale.SaleId;
//            return View("Index", vm);
//        }

//        /* ---------- Update Hold Order ---------- */
//        [HttpPost]
//        public async Task<IActionResult> UpdateHold(int saleId, POSViewModel vm)
//        {
//            var sale = await _db.Sales.Include(s => s.SaleDetails).FirstOrDefaultAsync(s => s.SaleId == saleId);
//            if (sale == null) return NotFound();

//            /* restore inventory from old items */
//            foreach (var old in sale.SaleDetails)
//            {
//                var inv = await _db.Inventorys.FirstAsync(i => i.MedicineId == old.MedicineId);
//                inv.QuantityOnHand += old.Quantity;
//            }
//            _db.SaleDetails.RemoveRange(sale.SaleDetails);
//            await _db.SaveChangesAsync();

//            /* apply new items */
//            foreach (var line in vm.Items)
//            {
//                var inv = await _db.Inventorys.FirstAsync(i => i.MedicineId == line.MedicineId);
//                if (inv.QuantityOnHand < line.Quantity)
//                {
//                    ModelState.AddModelError("", $"{line.MedicineName} now out of stock.");
//                    await LoadDropdowns();
//                    return View("Index", vm);
//                }
//                inv.QuantityOnHand -= line.Quantity;
//                _db.SaleDetails.Add(new SaleDetail
//                {
//                    SaleId = sale.SaleId,
//                    MedicineId = line.MedicineId,
//                    Quantity = line.Quantity,
//                    UnitPrice = line.UnitPrice,
//                    Discount = line.Discount,
//                });
//            }
//            sale.TotalAmount = vm.GrandTotal;
//            sale.Discount = vm.DiscountValue;
//            sale.PaymentStatus = vm.PaymentStatus;
//            sale.UpdatedAt = DateTime.Now;

//            if (vm.PaymentStatus == "Paid")
//            {
//                _db.Payments.Add(new Payment
//                {
//                    ReferenceType = "Sale",
//                    ReferenceId = sale.SaleId,
//                    TotalAmount = sale.TotalAmount,
//                    Status = "Completed",
//                    PaymentDate = DateTime.Now,
//                    CreatedAt = DateTime.Now
//                });
//            }
//            await _db.SaveChangesAsync();
//            return RedirectToAction("Receipt", new { id = sale.SaleId });
//        }

//        /* ---------- Receipt ---------- */
//        public async Task<IActionResult> Receipt(int id)
//        {
//            var sale = await _db.Sales.Include(s => s.SaleDetails).ThenInclude(d => d.Medicine)
//                                       .Include(s => s.Customer).Include(s => s.Staff)
//                                       .FirstOrDefaultAsync(s => s.SaleId == id);
//            return View(sale);
//        }

//        /* ---------- helper ---------- */
//        private async Task LoadDropdowns()
//        {
//            // POSController.cs  – inside LoadDropdowns()
//            ViewBag.MedicinesLite = await _db.Medicines
//                .Select(m => new {
//                    m.MedicineId,
//                    m.MedicineName,
//                    m.CategoryId,
//                    UnitPrice = m.SingleUnitPrice ?? m.CotanUnitPrice,
//                    m.SingleUnitPrice,
//                    m.CotanUnitPrice
//                })
//                .ToListAsync();

//            ViewBag.InventoryLite = await _db.Inventorys
//                .Select(i => new { i.MedicineId, i.QuantityOnHand })
//                .ToListAsync();

//            ViewBag.CategoriesLite = await _db.Categorys
//                .Select(c => new { c.CategoryId, c.CategoryName })
//                .ToListAsync();


//            ViewBag.Categories = await _db.Categorys.ToListAsync();
//            ViewBag.Medicines = await _db.Medicines.ToListAsync();
//            ViewBag.Customers = await _db.Customers.ToListAsync();
//            ViewBag.Staffs = await _db.Staffs.ToListAsync();
//        }
//    }
//}

#endregion
