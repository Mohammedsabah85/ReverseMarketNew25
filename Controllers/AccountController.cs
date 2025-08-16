using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReverseMarket.Data;
using ReverseMarket.Models;
using ReverseMarket.Services;
using System.ComponentModel.DataAnnotations;

namespace ReverseMarket.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWhatsAppService _whatsAppService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AccountController(ApplicationDbContext context, IWhatsAppService whatsAppService, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _whatsAppService = whatsAppService;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet]
        public IActionResult Login()
        {
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
                // تنظيف رقم الهاتف
                var cleanPhoneNumber = model.CountryCode + model.PhoneNumber.TrimStart('0');

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == cleanPhoneNumber);

                if (user != null)
                {
                    // مستخدم موجود - إرسال OTP للدخول
                    var otp = GenerateOTP();

                    // حفظ OTP في الجلسة
                    HttpContext.Session.SetString("OTP", otp);
                    HttpContext.Session.SetString("PhoneNumber", cleanPhoneNumber);
                    HttpContext.Session.SetString("LoginType", "ExistingUser");

                    // إرسال OTP عبر WhatsApp
                    await _whatsAppService.SendOTPAsync(cleanPhoneNumber, otp);

                    return RedirectToAction("VerifyOTP");
                }
                else
                {
                    // مستخدم جديد - إرسال رمز التحقق للتسجيل
                    var verificationCode = GenerateOTP();

                    // حفظ معلومات التحقق في الجلسة
                    HttpContext.Session.SetString("VerificationCode", verificationCode);
                    HttpContext.Session.SetString("PhoneNumber", cleanPhoneNumber);
                    HttpContext.Session.SetString("LoginType", "NewUser");

                    // إرسال رمز التحقق عبر WhatsApp
                    await _whatsAppService.SendOTPAsync(cleanPhoneNumber, verificationCode);

                    return RedirectToAction("VerifyPhone");
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult VerifyOTP()
        {
            var phoneNumber = HttpContext.Session.GetString("PhoneNumber");
            if (string.IsNullOrEmpty(phoneNumber))
            {
                return RedirectToAction("Login");
            }

            var model = new VerifyOTPViewModel();
            ViewBag.PhoneNumber = phoneNumber;
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
                        // تسجيل دخول ناجح
                        await LoginUserAsync(user);

                        // تنظيف الجلسة
                        ClearVerificationSession();

                        TempData["SuccessMessage"] = "مرحباً بك مرة أخرى!";
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
            var phoneNumber = HttpContext.Session.GetString("PhoneNumber");
            var loginType = HttpContext.Session.GetString("LoginType");

            if (string.IsNullOrEmpty(phoneNumber) || loginType != "NewUser")
            {
                return RedirectToAction("Login");
            }

            var model = new VerifyPhoneViewModel();
            ViewBag.PhoneNumber = phoneNumber;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyPhone(VerifyPhoneViewModel model)
        {
            if (ModelState.IsValid)
            {
                var storedCode = HttpContext.Session.GetString("VerificationCode");
                var phoneNumber = HttpContext.Session.GetString("PhoneNumber");

                if (model.VerificationCode == storedCode)
                {
                    // التحقق من الهاتف ناجح - انتقال لإنشاء الحساب
                    HttpContext.Session.SetString("PhoneVerified", "true");
                    return RedirectToAction("CreateAccount");
                }

                ModelState.AddModelError("", "رمز التحقق غير صحيح");
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> CreateAccount()
        {
            var phoneNumber = HttpContext.Session.GetString("PhoneNumber");
            var phoneVerified = HttpContext.Session.GetString("PhoneVerified");

            if (string.IsNullOrEmpty(phoneNumber) || phoneVerified != "true")
            {
                return RedirectToAction("Login");
            }

            ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
            ViewBag.PhoneNumber = phoneNumber;

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
                var phoneVerified = HttpContext.Session.GetString("PhoneVerified");

                if (string.IsNullOrEmpty(phoneNumber) || phoneVerified != "true")
                {
                    ModelState.AddModelError("", "جلسة التحقق منتهية الصلاحية");
                    return RedirectToAction("Login");
                }

                // التحقق من عدم وجود المستخدم (احتياط إضافي)
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);

                if (existingUser != null)
                {
                    ModelState.AddModelError("", "هذا الرقم مسجل مسبقاً");
                    return RedirectToAction("Login");
                }

                // التحقق من البريد الإلكتروني إذا تم إدخاله
                if (!string.IsNullOrEmpty(model.Email))
                {
                    var emailExists = await _context.Users
                        .AnyAsync(u => u.Email == model.Email);

                    if (emailExists)
                    {
                        ModelState.AddModelError("Email", "هذا البريد الإلكتروني مستخدم مسبقاً");
                        ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
                        return View(model);
                    }
                }

                // إنشاء المستخدم الجديد
                var user = new User
                {
                    PhoneNumber = phoneNumber,
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
                    IsPhoneVerified = true,
                    CreatedAt = DateTime.Now
                };

                // حفظ صورة الملف الشخصي إذا تم رفعها
                if (model.ProfileImage != null)
                {
                    var imagePath = await SaveProfileImageAsync(model.ProfileImage);
                    user.ProfileImage = imagePath;
                }

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // إضافة فئات المتجر إذا كان المستخدم بائع
                if (model.UserType == UserType.Seller && model.StoreCategories?.Any() == true)
                {
                    foreach (var categoryId in model.StoreCategories)
                    {
                        var storeCategory = new StoreCategory
                        {
                            UserId = user.Id,
                            CategoryId = categoryId,
                            CreatedAt = DateTime.Now
                        };
                        _context.StoreCategories.Add(storeCategory);
                    }
                    await _context.SaveChangesAsync();
                }

                // تسجيل دخول المستخدم
                await LoginUserAsync(user);

                // تنظيف الجلسة
                ClearVerificationSession();

                // رسالة ترحيب
                TempData["SuccessMessage"] = $"مرحباً بك {user.FirstName}! تم إنشاء حسابك بنجاح";

                return RedirectToAction("Index", "Home");
            }

            ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
            return View(model);
        }

        [HttpGet]
        public IActionResult ResendCode()
        {
            var phoneNumber = HttpContext.Session.GetString("PhoneNumber");
            var loginType = HttpContext.Session.GetString("LoginType");

            if (string.IsNullOrEmpty(phoneNumber))
            {
                return RedirectToAction("Login");
            }

            // إعادة إرسال الرمز المناسب حسب نوع العملية
            if (loginType == "ExistingUser")
            {
                return RedirectToAction("Login"); // إعادة بدء عملية الدخول
            }
            else
            {
                return RedirectToAction("Login"); // إعادة بدء عملية التسجيل
            }
        }

        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            TempData["InfoMessage"] = "تم تسجيل الخروج بنجاح";
            return RedirectToAction("Index", "Home");
        }

        #region Private Methods

        private async Task LoginUserAsync(User user)
        {
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("UserName", $"{user.FirstName} {user.LastName}");
            HttpContext.Session.SetString("UserType", user.UserType.ToString());
            HttpContext.Session.SetString("IsLoggedIn", "true");
        }

        private void ClearVerificationSession()
        {
            HttpContext.Session.Remove("OTP");
            HttpContext.Session.Remove("VerificationCode");
            HttpContext.Session.Remove("PhoneNumber");
            HttpContext.Session.Remove("LoginType");
            HttpContext.Session.Remove("PhoneVerified");
        }

        private string GenerateOTP()
        {
            var random = new Random();
            return random.Next(1000, 9999).ToString();
        }

        private async Task<string?> SaveProfileImageAsync(IFormFile image)
        {
            try
            {
                if (image == null || image.Length == 0)
                    return null;

                // التحقق من نوع الملف
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(image.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                    return null;

                // التحقق من حجم الملف (5MB)
                if (image.Length > 5 * 1024 * 1024)
                    return null;

                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "profiles");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(fileStream);
                }

                return $"/uploads/profiles/{fileName}";
            }
            catch (Exception)
            {
                // في حالة حدوث خطأ، إرجاع null
                return null;
            }
        }

        #endregion
    }
}