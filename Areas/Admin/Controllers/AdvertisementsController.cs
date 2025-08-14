using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReverseMarket.Data;
using ReverseMarket.Models;
using ReverseMarket.Areas.Admin.Models; // Add this line
using Microsoft.AspNetCore.Hosting;

using System.ComponentModel.DataAnnotations;

namespace ReverseMarket.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdvertisementsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AdvertisementsController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            var advertisements = await _context.Advertisements
                .OrderBy(a => a.Type)
                .ThenBy(a => a.DisplayOrder)
                .ToListAsync();

            return View(advertisements);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateAdvertisementViewModel model)
        {
            if (ModelState.IsValid)
            {
                string imagePath = "";

                if (model.Image != null)
                {
                    imagePath = await SaveImageAsync(model.Image);
                }

                var advertisement = new Advertisement
                {
                    Title = model.Title,
                    Description = model.Description,
                    ImagePath = imagePath,
                    LinkUrl = model.LinkUrl,
                    Type = model.Type,
                    DisplayOrder = model.DisplayOrder,
                    IsActive = model.IsActive,
                    StartDate = model.StartDate,
                    EndDate = model.EndDate
                };

                _context.Advertisements.Add(advertisement);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            return View(model);
        }

        private async Task<string> SaveImageAsync(IFormFile image)
        {
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "advertisements");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(fileStream);
            }

            return $"/uploads/advertisements/{fileName}";
        }
    }

    //public class CreateAdvertisementViewModel
    //{
    //    [Required]
    //    public string Title { get; set; }

    //    public string? Description { get; set; }

    //    [Required]
    //    public IFormFile Image { get; set; }

    //    public string? LinkUrl { get; set; }

    //    [Required]
    //    public AdvertisementType Type { get; set; }

    //    public int DisplayOrder { get; set; }

    //    public bool IsActive { get; set; } = true;

    //    [Required]
    //    public DateTime StartDate { get; set; }

    //    public DateTime? EndDate { get; set; }
    //}
}
