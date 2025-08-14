using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReverseMarket.Data;
using ReverseMarket.Models;
using ReverseMarket.Areas.Admin.Models;

namespace ReverseMarket.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class RequestsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RequestsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(RequestStatus? status = null, int page = 1)
        {
            var pageSize = 20;

            var query = _context.Requests
                .Include(r => r.User)
                .Include(r => r.Category)
                .Include(r => r.SubCategory1)
                .Include(r => r.SubCategory2)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(r => r.Status == status.Value);
            }

            var totalRequests = await query.CountAsync();
            var requests = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var model = new AdminRequestsViewModel
            {
                Requests = requests,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)totalRequests / pageSize),
                StatusFilter = status
            };

            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var request = await _context.Requests
                .Include(r => r.User)
                .Include(r => r.Category)
                .Include(r => r.SubCategory1)
                .Include(r => r.SubCategory2)
                .Include(r => r.Images)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return NotFound();
            }

            return View(request);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, RequestStatus status, string? adminNotes)
        {
            var request = await _context.Requests.FindAsync(id);
            if (request == null)
            {
                return NotFound();
            }

            request.Status = status;
            request.AdminNotes = adminNotes;

            if (status == RequestStatus.Approved)
            {
                request.ApprovedAt = DateTime.Now;

                // Notify user about approval
                await NotifyUserAboutApprovalAsync(request);

                // Notify relevant stores
                await NotifyStoresAboutNewRequestAsync(request);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id });
        }

        private async Task NotifyUserAboutApprovalAsync(Request request)
        {
            var user = await _context.Users.FindAsync(request.UserId);
            if (user != null && !string.IsNullOrEmpty(user.PhoneNumber))
            {
                var message = $"تم الموافقة على طلبك: {request.Title}";
                // Send WhatsApp notification
                Console.WriteLine($"WhatsApp to {user.PhoneNumber}: {message}");
            }
        }

        private async Task NotifyStoresAboutNewRequestAsync(Request request)
        {
            var relevantStores = await _context.StoreCategories
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
                    message += $"الرابط: [Request URL]";

                    // Send WhatsApp notification
                    Console.WriteLine($"WhatsApp to {store.PhoneNumber}: {message}");
                }
            }
        }
    }
}