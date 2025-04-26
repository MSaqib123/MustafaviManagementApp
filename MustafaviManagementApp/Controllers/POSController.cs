//===================================================================
//========================= Version # 3 =============================
//===================================================================
// 100 % file content – replace the whole controller
using MedicineStore.Data;
using MedicineStore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MustafaviManagementApp.ViewModels;
using Newtonsoft.Json;

namespace MustafaviManagementApp.Controllers
{
    public class POSController : Controller
    {
        private readonly AppDbContext _db;
        public POSController(AppDbContext db) => _db = db;

        /* ───────────── index view ───────────── */
        public async Task<IActionResult> Index()
        {
            await LoadDropdowns();
            return View(new POSViewModel { StaffId = 1 });
        }

        /* ───────────── pay / hold / recall update ───────────── */
        [HttpPost]
        public async Task<IActionResult> Index(
            string itemsJson,
            POSViewModel vm,
            string actionType,
            int? saleId              // null → new hold or pay,   id → update/pay
        )
        {
            await LoadDropdowns();

            if (!string.IsNullOrWhiteSpace(itemsJson))
                vm.Items = JsonConvert.DeserializeObject<List<POSLineItem>>(itemsJson)!;

            if (!vm.Items.Any())
            {
                ModelState.AddModelError("", "Cart is empty");
                return View(vm);
            }

            /* ── 1) if we’re paying a held order – free the reservation, delete it ── */
            if (actionType == "Pay" && saleId.HasValue)
            {
                var oldHeld = await _db.Sales
                    .Include(s => s.SaleDetails)
                    .FirstOrDefaultAsync(s => s.SaleId == saleId && s.IsHeld);

                if (oldHeld != null)
                {
                    foreach (var d in oldHeld.SaleDetails)
                    {
                        var inv = await _db.Inventorys.FirstAsync(i => i.MedicineId == d.MedicineId);
                        inv.ReservedQty -= d.Quantity;   // release reservation
                        inv.QuantityOnHand -= d.Quantity; // now ship it
                    }
                    _db.SaleDetails.RemoveRange(oldHeld.SaleDetails);
                    _db.Sales.Remove(oldHeld);
                    await _db.SaveChangesAsync();
                }
            }

            /* ── 2) create new Sale header (held or paid) ── */
            var sale = new Sale
            {
                CustomerId = vm.CustomerId,
                StaffId = vm.StaffId == 0 ? 1 : vm.StaffId,
                SaleDate = DateTime.Now,
                TotalAmount = vm.GrandTotal,
                Discount = vm.DiscountValue,
                PaymentMethod = vm.PaymentMethod,
                PaymentStatus = actionType == "Hold" ? "Pending" : "Paid",
                IsHeld = actionType == "Hold",
                CreatedAt = DateTime.Now
            };
            _db.Sales.Add(sale);
            await _db.SaveChangesAsync();             // we need SaleId

            /* ── 3) line-items ── */
            foreach (var l in vm.Items)
            {
                var inv = await _db.Inventorys.FirstAsync(i => i.MedicineId == l.MedicineId);

                if (actionType == "Hold")
                {
                    // reserve stock
                    if (inv.QuantityOnHand - inv.ReservedQty < l.Quantity)
                    {
                        ModelState.AddModelError("", $"{l.MedicineName} out of stock");
                        return View(vm);
                    }
                    inv.ReservedQty += l.Quantity;
                }
                else // Pay (new checkout)
                {
                    if (inv.QuantityOnHand - inv.ReservedQty < l.Quantity)
                    {
                        ModelState.AddModelError("", $"{l.MedicineName} out of stock");
                        return View(vm);
                    }
                    inv.QuantityOnHand -= l.Quantity;
                }

                _db.SaleDetails.Add(new SaleDetail
                {
                    SaleId = sale.SaleId,
                    MedicineId = l.MedicineId,
                    Quantity = l.Quantity,
                    UnitPrice = l.UnitPrice,
                    Discount = l.Discount
                });
            }

            /* ── 4) payment row only for Pay ── */
            if (actionType == "Pay")
            {
                _db.Payments.Add(new Payment
                {
                    ReferenceType = "Sale",
                    ReferenceId = sale.SaleId,
                    TotalAmount = sale.TotalAmount,
                    Status = "Completed",
                    PaymentDate = DateTime.Now,
                    CreatedAt = DateTime.Now
                });
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));     // fresh screen
        }

        /* ── update an existing held basket (Qty / items) ── */
        [HttpPost]
        public async Task<IActionResult> UpdateHold(
            int saleId, string itemsJson, POSViewModel vm)
        {
            if (!string.IsNullOrEmpty(itemsJson))
                vm.Items = JsonConvert.DeserializeObject<List<POSLineItem>>(itemsJson)!;

            var sale = await _db.Sales.Include(s => s.SaleDetails)
                                      .FirstOrDefaultAsync(s => s.SaleId == saleId && s.IsHeld);
            if (sale == null) return NotFound();

            /* 1) restore reservations */
            foreach (var old in sale.SaleDetails)
            {
                var inv = await _db.Inventorys.FirstAsync(i => i.MedicineId == old.MedicineId);
                inv.ReservedQty -= old.Quantity;
            }
            _db.SaleDetails.RemoveRange(sale.SaleDetails);
            await _db.SaveChangesAsync();

            /* 2) write new details & re-reserve */
            foreach (var l in vm.Items)
            {
                var inv = await _db.Inventorys.FirstAsync(i => i.MedicineId == l.MedicineId);
                if (inv.QuantityOnHand - inv.ReservedQty < l.Quantity)
                {
                    ModelState.AddModelError("", $"{l.MedicineName} out of stock");
                    return View("Index", vm);
                }
                inv.ReservedQty += l.Quantity;

                _db.SaleDetails.Add(new SaleDetail
                {
                    SaleId = sale.SaleId,
                    MedicineId = l.MedicineId,
                    Quantity = l.Quantity,
                    UnitPrice = l.UnitPrice,
                    Discount = l.Discount
                });
            }
            sale.TotalAmount = vm.GrandTotal;
            sale.Discount = vm.DiscountValue;
            sale.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        /* ── delete a held basket ── */
        [HttpPost]
        public async Task<IActionResult> DeleteHold(int id)
        {
            var sale = await _db.Sales.Include(s => s.SaleDetails)
                                      .FirstOrDefaultAsync(s => s.SaleId == id && s.IsHeld);
            if (sale == null) return NotFound();
            foreach (var d in sale.SaleDetails)
            {
                var inv = await _db.Inventorys.FirstAsync(i => i.MedicineId == d.MedicineId);
                inv.ReservedQty -= d.Quantity;
            }
            _db.SaleDetails.RemoveRange(sale.SaleDetails);
            _db.Sales.Remove(sale);
            await _db.SaveChangesAsync();
            return Ok();
        }

        /* ───── JSON helpers for UI ───── */
        [HttpGet]
        public async Task<IActionResult> GetHeldOrders() =>
            Json(await _db.Sales.Where(s => s.IsHeld)
                    .Select(s => new {
                        saleId = s.SaleId,
                        totalAmount = s.TotalAmount,
                        totalQty = s.SaleDetails.Sum(d => d.Quantity)
                    })
                    .OrderByDescending(s => s.saleId)
                    .ToListAsync());

        [HttpGet]
        public async Task<IActionResult> GetHeldOrder(int id)
        {
            var s = await _db.Sales.Include(x => x.SaleDetails)
                                   .ThenInclude(d => d.Medicine)
                                   .FirstOrDefaultAsync(x => x.SaleId == id && x.IsHeld);
            if (s == null) return NotFound();
            return Json(new
            {
                overallDiscount = s.Discount,
                discountIsPercent = true,
                vatPercent = 0,
                items = s.SaleDetails.Select(d => new {
                    medicineId = d.MedicineId,
                    medicineName = d.Medicine.MedicineName,
                    quantity = d.Quantity,
                    unitPrice = d.UnitPrice
                })
            });
        }

        /* ───── helpers ───── */
        private async Task LoadDropdowns()
        {
            // medicines for UI
            ViewBag.MedicinesLite = await _db.Medicines
                .Select(m => new {
                    m.MedicineId,
                    m.MedicineName,
                    m.CategoryId,
                    UnitPrice = m.SingleUnitPrice ?? m.CotanUnitPrice
                })
                .ToListAsync();

            // available stock = total on hand minus those reserved by held orders
            ViewBag.InventoryLite = await _db.Inventorys
                .Select(i => new {
                    i.MedicineId,
                    QuantityOnHand = i.QuantityOnHand - i.ReservedQty
                })
                .ToListAsync();

            ViewBag.CategoriesLite = await _db.Categorys
                .Select(c => new {
                    c.CategoryId,
                    c.CategoryName
                })
                .ToListAsync();

            // for dropdown lists (if you ever need them)
            ViewBag.Categories = await _db.Categorys.ToListAsync();
            ViewBag.Medicines = await _db.Medicines.ToListAsync();
            ViewBag.Customers = await _db.Customers.ToListAsync();
            ViewBag.Staffs = await _db.Staffs.ToListAsync();
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
