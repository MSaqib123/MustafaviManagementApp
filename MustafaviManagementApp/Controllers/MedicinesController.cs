using MedicineStore.Data;
using MedicineStore.Models;
using Microsoft.AspNetCore.Hosting;          // for IWebHostEnvironment
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MustafaviManagementApp.Services;
using System.IO;

public class MedicinesController : Controller
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;      // ⭐
    private readonly IFileStorageService _storage;


    public MedicinesController(AppDbContext context, IWebHostEnvironment env, IFileStorageService storage)
    {
        _context = context;
        _env = env;
        _storage = storage;

    }


    private void PopulateDropdowns()
    {
        ViewBag.Categories = new SelectList(
            _context.Categorys
                    .OrderBy(c => c.CategoryName)
                    .ToList(),                // <-- materialize here
            "CategoryId",
            "CategoryName"
        );
        ViewBag.ParentCategories = new SelectList(
            _context.ParentCategories
                    .OrderBy(c => c.ParentCategoryName)
                    .ToList(),                // <-- materialize here
            "ParentCategoryId",
            "ParentCategoryName"
        );
        ViewBag.Stores = new SelectList(
            _context.Stores
                    .OrderBy(s => s.StoreName)
                    .ToList(),                // <-- materialize here
            "StoreId",
            "StoreName"
        );
    }


    // GET: Medicines
    public async Task<IActionResult> Index()
    {
        var meds = await _context.Medicines
            .Include(m => m.ParentCategory)
            .Include(m => m.Category)
            .Include(m => m.Store)
            .ToListAsync();
        return View(meds);
    }

    // GET: Medicines/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var med = await _context.Medicines
            .Include(m => m.Category)
            .Include(m => m.Store)
            .FirstOrDefaultAsync(m => m.MedicineId == id);

        if (med == null) return NotFound();
        return View(med);
    }


    // --------------  CREATE  --------------
    [HttpGet]
    public IActionResult Create()
    {
        Medicine m = new Medicine();
        PopulateDropdowns();
        return View(m);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Medicine item)
    {
        PopulateDropdowns();
        if (!ModelState.IsValid) return View(item);

        if (!ModelState.IsValid) return View(item);

        if (item.ImageFile != null)
            item.Image = await _storage.SaveAsync(item.ImageFile, "medicines");


        item.CreatedAt = DateTime.Now;
        _context.Add(item);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // --------------  EDIT  --------------
    // GET: Medicines/Edit/5
    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var item = await _context.Medicines.FindAsync(id);
        if (item == null) return NotFound();

        PopulateDropdowns();
        return View(item);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Medicine item)
    {
        PopulateDropdowns();
        if (id != item.MedicineId) return NotFound();
        if (!ModelState.IsValid) return View(item);

        var dbItem = await _context.Medicines.AsNoTracking()
                                             .FirstOrDefaultAsync(m => m.MedicineId == id);
        if (dbItem is null) return NotFound();

        // If a new file uploaded, delete old and save new
        if (item.ImageFile != null)
        {
            _storage.Delete(dbItem.Image);  // remove old
            item.Image = await _storage.SaveAsync(item.ImageFile, "medicines");
        }
        else
        {
            item.Image = dbItem.Image;      // keep previous
        }

        item.UpdatedAt = DateTime.Now;
        _context.Update(item);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // --------------  DELETE  --------------
    // GET: Medicines/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var item = await _context.Medicines
            .Include(m => m.Category)
            .Include(m => m.Store)
            .FirstOrDefaultAsync(m => m.MedicineId == id);

        if (item == null) return NotFound();
        return View(item);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var med = await _context.Medicines.FindAsync(id);
        if (med != null)
        {
            _storage.Delete(med.Image);
            _context.Medicines.Remove(med);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }



    public IActionResult GetChildCategories(int parentId)
    {
        var childCategories = _context.CategoryParentCategories
            .Where(c => c.ParentCategoryId == parentId)
            .Select(c => new SelectListItem
            {
                Value = c.CategoryId.ToString(),
                Text = c.Category.CategoryName
            }).ToList();

        return Json(childCategories);
    }

}


//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using MedicineStore.Data;
//using MedicineStore.Models;
//using System.Threading.Tasks;
//using System.Linq;
//using Microsoft.AspNetCore.Mvc.Rendering;

//namespace MedicineStore.Controllers
//{
//    public class MedicinesController : Controller
//    {
//        private readonly AppDbContext _context;

//        public MedicinesController(AppDbContext context)
//            => _context = context;

//        // Helper to populate the dropdowns
//        private void PopulateDropdowns()
//        {
//            ViewBag.Categories = new SelectList(
//                _context.Categorys
//                        .OrderBy(c => c.CategoryName)
//                        .ToList(),                // <-- materialize here
//                "CategoryId",
//                "CategoryName"
//            );
//            ViewBag.Stores = new SelectList(
//                _context.Stores
//                        .OrderBy(s => s.StoreName)
//                        .ToList(),                // <-- materialize here
//                "StoreId",
//                "StoreName"
//            );
//        }

//        // GET: Medicines
//        public async Task<IActionResult> Index()
//        {
//            var meds = await _context.Medicines
//                .Include(m => m.Category)
//                .Include(m => m.Store)
//                .ToListAsync();
//            return View(meds);
//        }

//        // GET: Medicines/Details/5
//        public async Task<IActionResult> Details(int? id)
//        {
//            if (id == null) return NotFound();

//            var med = await _context.Medicines
//                .Include(m => m.Category)
//                .Include(m => m.Store)
//                .FirstOrDefaultAsync(m => m.MedicineId == id);

//            if (med == null) return NotFound();
//            return View(med);
//        }

//        // GET: Medicines/Create
//        public IActionResult Create()
//        {
//            Medicine m = new Medicine();
//            PopulateDropdowns();
//            return View(m);
//        }

//        // POST: Medicines/Create
//        [HttpPost, ValidateAntiForgeryToken]
//        public async Task<IActionResult> Create(Medicine item)
//        {
//            PopulateDropdowns();
//            if (!ModelState.IsValid)
//                return View(item);

//            item.CreatedAt = DateTime.Now;
//            _context.Add(item);
//            await _context.SaveChangesAsync();
//            return RedirectToAction(nameof(Index));
//        }

//        // GET: Medicines/Edit/5
//        public async Task<IActionResult> Edit(int? id)
//        {
//            if (id == null) return NotFound();

//            var item = await _context.Medicines.FindAsync(id);
//            if (item == null) return NotFound();

//            PopulateDropdowns();
//            return View(item);
//        }

//        // POST: Medicines/Edit/5
//        [HttpPost, ValidateAntiForgeryToken]
//        public async Task<IActionResult> Edit(int id, Medicine item)
//        {
//            PopulateDropdowns();
//            if (id != item.MedicineId) return NotFound();
//            if (!ModelState.IsValid) return View(item);

//            try
//            {
//                item.UpdatedAt = DateTime.Now;
//                _context.Update(item);
//                await _context.SaveChangesAsync();
//            }
//            catch (DbUpdateConcurrencyException)
//            {
//                if (!_context.Medicines.Any(e => e.MedicineId == id))
//                    return NotFound();
//                throw;
//            }
//            return RedirectToAction(nameof(Index));
//        }

//        // GET: Medicines/Delete/5
//        public async Task<IActionResult> Delete(int? id)
//        {
//            if (id == null) return NotFound();

//            var item = await _context.Medicines
//                .Include(m => m.Category)
//                .Include(m => m.Store)
//                .FirstOrDefaultAsync(m => m.MedicineId == id);

//            if (item == null) return NotFound();
//            return View(item);
//        }

//        // POST: Medicines/Delete/5
//        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
//        public async Task<IActionResult> DeleteConfirmed(int id)
//        {
//            var item = await _context.Medicines.FindAsync(id);
//            _context.Medicines.Remove(item!);
//            await _context.SaveChangesAsync();
//            return RedirectToAction(nameof(Index));
//        }
//    }
//}
