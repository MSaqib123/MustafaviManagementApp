using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using MedicineStore.Data;
using MedicineStore.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using MustafaviManagementApp.Models;
using MustafaviManagementApp.ViewModels;

namespace MedicineStore.Controllers
{
    public class CategorysParentCategoryController : Controller
    {
        private readonly AppDbContext _ctx;
        public CategorysParentCategoryController(AppDbContext ctx) => _ctx = ctx;

        /* ========== INDEX ========== */
        public async Task<IActionResult> Index()
        {
            var brands = await _ctx.ParentCategories
                                   .Include(p => p.CategoryParentCategories)
                                   .ThenInclude(cp => cp.Category)
                                   .ToListAsync();
            return View(brands);
        }

        /* ========== CREATE ========== */
        public IActionResult Create()
        {
            // show only brands that are not yet mapped
            ViewBag.ParentCategories = new SelectList(
                _ctx.ParentCategories
                    .Where(p => !p.CategoryParentCategories.Any())
                    .Select(p => new { p.ParentCategoryId, p.ParentCategoryName }),
                "ParentCategoryId", "ParentCategoryName");

            var vm = new ParentCategoryMapVM
            {
                CategoryOptions = _ctx.Categorys
                                      .Select(c => new SelectListItem
                                      {
                                          Value = c.CategoryId.ToString(),
                                          Text = c.CategoryName
                                      })
                                      .ToList()
            };
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ParentCategoryMapVM vm)
        {
            if (!ModelState.IsValid)
            {
                vm.CategoryOptions = _ctx.Categorys
                                         .Select(c => new SelectListItem
                                         {
                                             Value = c.CategoryId.ToString(),
                                             Text = c.CategoryName
                                         }).ToList();

                ViewBag.ParentCategories = new SelectList(
                    _ctx.ParentCategories.Where(p => !p.CategoryParentCategories.Any()),
                    "ParentCategoryId", "ParentCategoryName");
                return View(vm);
            }

            // prevent double posting
            if (await _ctx.CategoryParentCategories
                          .AnyAsync(cp => cp.ParentCategoryId == vm.ParentCategoryId))
            {
                ModelState.AddModelError(string.Empty, "This Parent Category already has a mapping. Please edit it instead.");
                vm.CategoryOptions = _ctx.Categorys.Select(c => new SelectListItem { Value = c.CategoryId.ToString(), Text = c.CategoryName }).ToList();
                ViewBag.ParentCategories = new SelectList(
                    _ctx.ParentCategories.Where(p => !p.CategoryParentCategories.Any()),
                    "ParentCategoryId", "ParentCategoryName");
                return View(vm);
            }

            foreach (var cid in vm.SelectedCategoryIds.Distinct())
                _ctx.CategoryParentCategories.Add(new CategoryParentCategory
                {
                    ParentCategoryId = vm.ParentCategoryId,
                    CategoryId = cid
                });

            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        /* ========== EDIT ========== */
        public async Task<IActionResult> Edit(int? id)
        {
            if (id is null) return NotFound();
            var brand = await _ctx.ParentCategories
                                  .Include(p => p.CategoryParentCategories)
                                  .FirstOrDefaultAsync(p => p.ParentCategoryId == id);
            if (brand is null) return NotFound();

            var vm = new ParentCategoryMapVM
            {
                ParentCategoryId = brand.ParentCategoryId,
                ParentCategoryName = brand.ParentCategoryName,
                SelectedCategoryIds = brand.CategoryParentCategories
                                         .Select(cp => cp.CategoryId).ToList(),
                CategoryOptions = _ctx.Categorys.Select(c => new SelectListItem
                { Value = c.CategoryId.ToString(), Text = c.CategoryName })
                                   .ToList()
            };
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ParentCategoryMapVM vm)
        {
            if (id != vm.ParentCategoryId) return NotFound();

            if (!vm.SelectedCategoryIds.Any())
                ModelState.AddModelError(string.Empty, "Select at least one category.");

            if (!ModelState.IsValid)
            {
                vm.CategoryOptions = _ctx.Categorys.Select(c => new SelectListItem
                { Value = c.CategoryId.ToString(), Text = c.CategoryName }).ToList();
                return View(vm);
            }

            var existing = await _ctx.CategoryParentCategories
                                     .Where(cp => cp.ParentCategoryId == id)
                                     .ToListAsync();

            // remove unchecked
            var remove = existing.Where(cp => !vm.SelectedCategoryIds.Contains(cp.CategoryId));
            _ctx.CategoryParentCategories.RemoveRange(remove);

            // add newly checked
            var addIds = vm.SelectedCategoryIds
                           .Where(cid => !existing.Any(cp => cp.CategoryId == cid));
            foreach (var cid in addIds)
                _ctx.CategoryParentCategories.Add(new CategoryParentCategory
                {
                    ParentCategoryId = id,
                    CategoryId = cid
                });

            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        /* ========== DELETE (all mappings for one brand) ========== */
        public async Task<IActionResult> Delete(int? id)
        {
            if (id is null) return NotFound();
            var brand = await _ctx.ParentCategories
                                  .Include(p => p.CategoryParentCategories)
                                  .ThenInclude(cp => cp.Category)
                                  .FirstOrDefaultAsync(p => p.ParentCategoryId == id);
            return brand is null ? NotFound() : View(brand);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var mappings = _ctx.CategoryParentCategories.Where(cp => cp.ParentCategoryId == id);
            _ctx.CategoryParentCategories.RemoveRange(mappings);
            await _ctx.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        /* ========== AJAX helper for modal ========== */
        [HttpGet]
        public async Task<IActionResult> GetCategories(int id)
        {
            var cats = await _ctx.CategoryParentCategories
                                 .Where(cp => cp.ParentCategoryId == id)
                                 .Select(cp => cp.Category.CategoryName)
                                 .ToListAsync();
            return Json(cats);
        }
    }
}
