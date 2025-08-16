using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReverseMarket.Data;
using ReverseMarket.Models;
using System.ComponentModel.DataAnnotations;

namespace ReverseMarket.Controllers
{
    public class RequestsController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public RequestsController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _dbContext = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index(string search, int? categoryId, int page = 1)
        {
            var pageSize = 12;

            var query = _dbContext.Requests
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

            var model = new ReverseMarket.Models.RequestsViewModel
            {
                Requests = requests,
                Categories = await _dbContext.Categories.Where(c => c.IsActive).ToListAsync(),
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)totalRequests / pageSize),
                Search = search,
                SelectedCategoryId = categoryId
            };

            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var request = await _dbContext.Requests
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
            // التحقق من تسجيل الدخول
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                TempData["ErrorMessage"] = "يجب تسجيل الدخول أولاً";
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Categories = await _dbContext.Categories.Where(c => c.IsActive).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateRequestViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    TempData["ErrorMessage"] = "جلسة المستخدم منتهية الصلاحية";
                    return RedirectToAction("Login", "Account");
                }

                try
                {
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
                        Status = RequestStatus.Pending,
                        CreatedAt = DateTime.Now
                    };

                    _dbContext.Requests.Add(request);
                    await _dbContext.SaveChangesAsync();

                    // معالجة رفع الصور
                    if (model.Images != null && model.Images.Any())
                    {
                        await SaveRequestImagesAsync(request.Id, model.Images);
                    }

                    // إشعار المتاجر المتخصصة
                    await NotifyStoresAsync(request);

                    TempData["SuccessMessage"] = "تم إرسال طلبك بنجاح! سيتم مراجعته والموافقة عليه في أقرب وقت.";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    // تسجيل الخطأ
                    Console.WriteLine($"خطأ في إنشاء الطلب: {ex.Message}");
                    TempData["ErrorMessage"] = "حدث خطأ أثناء إرسال الطلب. يرجى المحاولة مرة أخرى.";
                }
            }

            ViewBag.Categories = await _dbContext.Categories.Where(c => c.IsActive).ToListAsync();
            return View(model);
        }

        private async Task SaveRequestImagesAsync(int requestId, List<IFormFile> images)
        {
            try
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "requests");

                // إنشاء المجلد إذا لم يكن موجوداً
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var maxFileSize = 5 * 1024 * 1024; // 5 MB

                foreach (var image in images.Take(3)) // أقصى 3 صور
                {
                    if (image?.Length > 0)
                    {
                        // التحقق من نوع الملف
                        var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
                        if (!allowedExtensions.Contains(extension))
                        {
                            continue; // تجاهل الملفات غير المدعومة
                        }

                        // التحقق من حجم الملف
                        if (image.Length > maxFileSize)
                        {
                            continue; // تجاهل الملفات الكبيرة
                        }

                        // إنشاء اسم ملف فريد
                        var fileName = $"{Guid.NewGuid()}{extension}";
                        var filePath = Path.Combine(uploadsFolder, fileName);

                        // حفظ الصورة
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await image.CopyToAsync(fileStream);
                        }

                        // حفظ معلومات الصورة في قاعدة البيانات
                        var requestImage = new RequestImage
                        {
                            RequestId = requestId,
                            ImagePath = $"/uploads/requests/{fileName}",
                            CreatedAt = DateTime.Now
                        };

                        _dbContext.RequestImages.Add(requestImage);
                    }
                }

                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"خطأ في حفظ الصور: {ex.Message}");
            }
        }

        private async Task NotifyStoresAsync(Request request)
        {
            try
            {
                var relevantStores = await _dbContext.StoreCategories
                    .Include(sc => sc.User)
                    .Where(sc => sc.CategoryId == request.CategoryId ||
                               sc.SubCategory1Id == request.SubCategory1Id ||
                               sc.SubCategory2Id == request.SubCategory2Id)
                    .Select(sc => sc.User)
                    .Where(u => u.UserType == UserType.Seller)
                    .Distinct()
                    .ToListAsync();

                foreach (var store in relevantStores)
                {
                    if (!string.IsNullOrEmpty(store.PhoneNumber))
                    {
                        var message = $"طلب جديد في متجركم: {request.Title}\n";
                        message += $"الموقع: {request.City} - {request.District}\n";
                        message += $"الرابط: {Url.Action("Details", "Requests", new { id = request.Id }, Request.Scheme)}";

                        Console.WriteLine($"WhatsApp to {store.PhoneNumber}: {message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"خطأ في إرسال الإشعارات: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSubCategories1(int categoryId)
        {
            var subCategories = await _dbContext.SubCategories1
                .Where(sc => sc.CategoryId == categoryId && sc.IsActive)
                .Select(sc => new { id = sc.Id, name = sc.Name })
                .ToListAsync();

            return Json(subCategories);
        }

        [HttpGet]
        public async Task<IActionResult> GetSubCategories2(int subCategory1Id)
        {
            var subCategories = await _dbContext.SubCategories2
                .Where(sc => sc.SubCategory1Id == subCategory1Id && sc.IsActive)
                .Select(sc => new { id = sc.Id, name = sc.Name })
                .ToListAsync();

            return Json(subCategories);
        }
    }
}