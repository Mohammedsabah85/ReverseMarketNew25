using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReverseMarket.Data;
using ReverseMarket.Models;
using System.ComponentModel.DataAnnotations;

namespace ReverseMarket.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            // إنشاء model فارغ لتجنب NullReferenceException
            var model = new LoginViewModel
            {
                CountryCode = "+964"
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == model.PhoneNumber);

                if (user != null)
                {
                    // Existing user - send OTP
                    // Generate and send OTP via WhatsApp
                    var otp = GenerateOTP();

                    // Store OTP in session for verification
                    HttpContext.Session.SetString("OTP", otp);
                    HttpContext.Session.SetString("PhoneNumber", model.PhoneNumber);

                    return RedirectToAction("VerifyOTP");
                }
                else
                {
                    // New user - redirect to phone verification
                    HttpContext.Session.SetString("PhoneNumber", model.PhoneNumber);
                    return RedirectToAction("VerifyPhone");
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult VerifyOTP()
        {
            var model = new VerifyOTPViewModel();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOTP(VerifyOTPViewModel model)
        {
            if (ModelState.IsValid)
            {
                var storedOTP = HttpContext.Session.GetString("OTP");
                var phoneNumber = HttpContext.Session.GetString("PhoneNumber");

                if (model.OTP == storedOTP)
                {
                    var user = await _context.Users
                        .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);

                    if (user != null)
                    {
                        // Login successful
                        HttpContext.Session.SetInt32("UserId", user.Id);
                        HttpContext.Session.SetString("UserName", $"{user.FirstName} {user.LastName}");
                        HttpContext.Session.SetString("UserType", user.UserType.ToString());

                        // Clear OTP session
                        HttpContext.Session.Remove("OTP");
                        HttpContext.Session.Remove("PhoneNumber");

                        return RedirectToAction("Index", "Home");
                    }
                }

                ModelState.AddModelError("", "رمز التحقق غير صحيح");
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult VerifyPhone()
        {
            var model = new VerifyPhoneViewModel();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VerifyPhone(VerifyPhoneViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Generate and send WhatsApp verification code
                var verificationCode = GenerateOTP();

                // Store verification code in session
                HttpContext.Session.SetString("VerificationCode", verificationCode);

                return RedirectToAction("CreateAccount");
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> CreateAccount()
        {
            ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
            var model = new CreateAccountViewModel();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAccount(CreateAccountViewModel model)
        {
            if (ModelState.IsValid)
            {
                var phoneNumber = HttpContext.Session.GetString("PhoneNumber");

                var user = new User
                {
                    PhoneNumber = phoneNumber ?? "",
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    DateOfBirth = model.DateOfBirth,
                    Gender = model.Gender,
                    City = model.City,
                    District = model.District,
                    Location = model.Location,
                    Email = model.Email,
                    UserType = model.UserType,
                    StoreName = model.StoreName,
                    StoreDescription = model.StoreDescription,
                    WebsiteUrl1 = model.WebsiteUrl1,
                    WebsiteUrl2 = model.WebsiteUrl2,
                    WebsiteUrl3 = model.WebsiteUrl3,
                    IsPhoneVerified = true
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Add store categories if user is a seller
                if (model.UserType == UserType.Seller && model.StoreCategories?.Any() == true)
                {
                    foreach (var categoryId in model.StoreCategories)
                    {
                        var storeCategory = new StoreCategory
                        {
                            UserId = user.Id,
                            CategoryId = categoryId
                        };
                        _context.StoreCategories.Add(storeCategory);
                    }
                    await _context.SaveChangesAsync();
                }

                // Login user
                HttpContext.Session.SetInt32("UserId", user.Id);
                HttpContext.Session.SetString("UserName", $"{user.FirstName} {user.LastName}");
                HttpContext.Session.SetString("UserType", user.UserType.ToString());

                return RedirectToAction("Index", "Home");
            }

            ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
            return View(model);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        private string GenerateOTP()
        {
            var random = new Random();
            return random.Next(1000, 9999).ToString();
        }
    }
}