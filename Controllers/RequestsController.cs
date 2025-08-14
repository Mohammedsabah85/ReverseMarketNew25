using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReverseMarket.Data;
using ReverseMarket.Models;
using System.ComponentModel.DataAnnotations;

namespace ReverseMarket.Controllers
{
    public class RequestsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RequestsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string search, int? categoryId, int page = 1)
        {
            var pageSize = 12;

            var query = _context.Requests
                .Where(r => r.Status == RequestStatus.Approved)
                .Include(r => r.Category)
                .Include(r => r.SubCategory1)
                .Include(r => r.SubCategory2)
                .Include(r => r.Images)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(r => r.Title.Contains(search) || r.Description.Contains(search));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(r => r.CategoryId == categoryId.Value);
            }

            var totalRequests = await query.CountAsync();
            var requests = await query
                .OrderByDescending(r => r.ApprovedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var model = new RequestsViewModel
            {
                Requests = requests,
                Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync(),
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)totalRequests / pageSize),
                Search = search,
                SelectedCategoryId = categoryId
            };

            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var request = await _context.Requests
                .Include(r => r.Category)
                .Include(r => r.SubCategory1)
                .Include(r => r.SubCategory2)
                .Include(r => r.Images)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id && r.Status == RequestStatus.Approved);

            if (request == null)
            {
                return NotFound();
            }

            return View(request);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateRequestViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if user is logged in
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    return RedirectToAction("Login", "Account");
                }

                var request = new Request
                {
                    Title = model.Title,
                    Description = model.Description,
                    CategoryId = model.CategoryId,
                    SubCategory1Id = model.SubCategory1Id,
                    SubCategory2Id = model.SubCategory2Id,
                    City = model.City,
                    District = model.District,
                    Location = model.Location,
                    UserId = userId.Value,
                    Status = RequestStatus.Pending
                };

                _context.Requests.Add(request);
                await _context.SaveChangesAsync();

                // Handle image uploads here
                // Save images and create RequestImage records

                // Notify relevant stores via WhatsApp
                await NotifyStoresAsync(request);

                TempData["SuccessMessage"] = "سوف يتم عرض طلبك بأسرع وقت بعد التحقق منه";
                return RedirectToAction("Index");
            }

            ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
            return View(model);
        }

        private async Task NotifyStoresAsync(Request request)
        {
            // Get stores that work in the same category
            var relevantStores = await _context.StoreCategories
                .Include(sc => sc.User)
                .Where(sc => sc.CategoryId == request.CategoryId ||
                           sc.SubCategory1Id == request.SubCategory1Id ||
                           sc.SubCategory2Id == request.SubCategory2Id)
                .Select(sc => sc.User)
                .Where(u => u.UserType == UserType.Seller)
                .Distinct()
                .ToListAsync();

            // Send WhatsApp notifications to relevant stores
            foreach (var store in relevantStores)
            {
                if (!string.IsNullOrEmpty(store.PhoneNumber))
                {
                    var message = $"طلب جديد في متجركم: {request.Title}\n";
                    message += $"الرابط: {Url.Action("Details", "Requests", new { id = request.Id }, Request.Scheme)}";

                    // Here you would integrate with WhatsApp API
                    // For now, we'll just log it
                    Console.WriteLine($"WhatsApp to {store.PhoneNumber}: {message}");
                }
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSubCategories1(int categoryId)
        {
            var subCategories = await _context.SubCategories1
                .Where(sc => sc.CategoryId == categoryId && sc.IsActive)
                .Select(sc => new { id = sc.Id, name = sc.Name })
                .ToListAsync();

            return Json(subCategories);
        }

        [HttpGet]
        public async Task<IActionResult> GetSubCategories2(int subCategory1Id)
        {
            var subCategories = await _context.SubCategories2
                .Where(sc => sc.SubCategory1Id == subCategory1Id && sc.IsActive)
                .Select(sc => new { id = sc.Id, name = sc.Name })
                .ToListAsync();

            return Json(subCategories);
        }
    }

    public class RequestsViewModel
    {
        public List<Request> Requests { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string? Search { get; set; }
        public int? SelectedCategoryId { get; set; }
    }

    public class CreateRequestViewModel
    {
        [Required]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public int CategoryId { get; set; }

        public int? SubCategory1Id { get; set; }

        public int? SubCategory2Id { get; set; }

        [Required]
        public string City { get; set; }

        [Required]
        public string District { get; set; }

        public string? Location { get; set; }

        public List<IFormFile>? Images { get; set; }
    }
}
