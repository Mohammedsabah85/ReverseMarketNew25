using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReverseMarket.Data;
using ReverseMarket.Models;

namespace ReverseMarket.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CategoriesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                .Include(c => c.SubCategories1)
                .ThenInclude(sc => sc.SubCategories2)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(categories);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            if (ModelState.IsValid)
            {
                _context.Categories.Add(category);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            return View(category);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Category category)
        {
            if (ModelState.IsValid)
            {
                _context.Update(category);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            return View(category);
        }

        // تصحيح routing للفئات الفرعية الأولى
        [HttpGet]
        [Route("Admin/Categories/CreateSubCategory1/{categoryId:int}")]
        public async Task<IActionResult> CreateSubCategory1(int categoryId)
        {
            var category = await _context.Categories.FindAsync(categoryId);
            if (category == null)
            {
                return NotFound();
            }

            ViewBag.Category = category;
            var model = new SubCategory1 { CategoryId = categoryId };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/Categories/CreateSubCategory1/{categoryId:int}")]
        public async Task<IActionResult> CreateSubCategory1(SubCategory1 subCategory)
        {
            if (ModelState.IsValid)
            {
                _context.SubCategories1.Add(subCategory);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            var category = await _context.Categories.FindAsync(subCategory.CategoryId);
            ViewBag.Category = category;
            return View(subCategory);
        }

        // تصحيح routing للفئات الفرعية الثانية
        [HttpGet]
        [Route("Admin/Categories/CreateSubCategory2/{subCategory1Id:int}")]
        public async Task<IActionResult> CreateSubCategory2(int subCategory1Id)
        {
            var subCategory1 = await _context.SubCategories1
                .Include(sc => sc.Category)
                .FirstOrDefaultAsync(sc => sc.Id == subCategory1Id);

            if (subCategory1 == null)
            {
                return NotFound();
            }

            ViewBag.SubCategory1 = subCategory1;
            var model = new SubCategory2 { SubCategory1Id = subCategory1Id };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/Categories/CreateSubCategory2/{subCategory1Id:int}")]
        public async Task<IActionResult> CreateSubCategory2(SubCategory2 subCategory)
        {
            if (ModelState.IsValid)
            {
                _context.SubCategories2.Add(subCategory);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }

            var subCategory1 = await _context.SubCategories1
                .Include(sc => sc.Category)
                .FirstOrDefaultAsync(sc => sc.Id == subCategory.SubCategory1Id);

            ViewBag.SubCategory1 = subCategory1;
            return View(subCategory);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }
    }
}