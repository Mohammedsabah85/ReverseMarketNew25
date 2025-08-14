using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReverseMarket.Data;
using ReverseMarket.Models;

namespace ReverseMarket.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var model = new AdminDashboardViewModel
            {
                TotalUsers = await _context.Users.CountAsync(),
                TotalRequests = await _context.Requests.CountAsync(),
                PendingRequests = await _context.Requests.CountAsync(r => r.Status == RequestStatus.Pending),
                TotalStores = await _context.Users.CountAsync(u => u.UserType == UserType.Seller),
                TotalCategories = await _context.Categories.CountAsync(),

                RecentRequests = await _context.Requests
                    .Include(r => r.User)
                    .Include(r => r.Category)
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(10)
                    .ToListAsync()
            };

            return View(model);
        }
    }

    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalRequests { get; set; }
        public int PendingRequests { get; set; }
        public int TotalStores { get; set; }
        public int TotalCategories { get; set; }
        public List<Request> RecentRequests { get; set; } = new();
    }
}
