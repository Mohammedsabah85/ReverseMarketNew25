using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReverseMarket.Data;
using ReverseMarket.Models;

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
            var model = new HomeViewModel
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
    }

    public class HomeViewModel
    {
        public List<Advertisement> Advertisements { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public List<Request> RecentRequests { get; set; } = new();
        public SiteSettings? SiteSettings { get; set; }
    }
}
