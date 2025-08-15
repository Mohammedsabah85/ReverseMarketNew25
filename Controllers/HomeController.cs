using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReverseMarket.Data;
using ReverseMarket.Models; // تأكد من استيراد الـ Models namespace
using System.Diagnostics;

namespace ReverseMarket.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // استخدم النوع الصحيح من ReverseMarket.Models
            var model = new ReverseMarket.Models.HomeViewModel
            {
                Advertisements = await _context.Advertisements
                    .Where(a => a.IsActive && a.Type == AdvertisementType.Banner)
                    .OrderBy(a => a.DisplayOrder)
                    .ToListAsync(),

                Categories = await _context.Categories
                    .Where(c => c.IsActive)
                    .Take(8)
                    .ToListAsync(),

                RecentRequests = await _context.Requests
                    .Where(r => r.Status == RequestStatus.Approved)
                    .Include(r => r.Category)
                    .Include(r => r.Images)
                    .OrderByDescending(r => r.ApprovedAt)
                    .Take(12)
                    .ToListAsync(),

                SiteSettings = await _context.SiteSettings.FirstOrDefaultAsync()
            };

            return View(model);
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}