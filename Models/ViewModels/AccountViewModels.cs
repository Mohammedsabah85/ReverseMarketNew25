using System.ComponentModel.DataAnnotations;

namespace ReverseMarket.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "رقم الهاتف مطلوب")]
        [Phone(ErrorMessage = "رقم الهاتف غير صحيح")]
        public string PhoneNumber { get; set; } = "";

        public string CountryCode { get; set; } = "+964";

        [Required(ErrorMessage = "يجب الموافقة على الشروط والأحكام")]
        public bool AcceptTerms { get; set; }
    }

    public class VerifyOTPViewModel
    {
        [Required(ErrorMessage = "رمز التحقق مطلوب")]
        [StringLength(4, MinimumLength = 4, ErrorMessage = "رمز التحقق يجب أن يكون 4 أرقام")]
        [RegularExpression(@"^\d{4}$", ErrorMessage = "رمز التحقق يجب أن يحتوي على أرقام فقط")]
        public string OTP { get; set; } = "";
    }

    public class VerifyPhoneViewModel
    {
        [Required(ErrorMessage = "رمز التأكيد مطلوب")]
        [StringLength(4, MinimumLength = 4, ErrorMessage = "رمز التأكيد يجب أن يكون 4 أرقام")]
        [RegularExpression(@"^\d{4}$", ErrorMessage = "رمز التأكيد يجب أن يحتوي على أرقام فقط")]
        public string VerificationCode { get; set; } = "";
    }

    public class CreateAccountViewModel
    {
        [Required(ErrorMessage = "الاسم الأول مطلوب")]
        [StringLength(100, ErrorMessage = "الاسم الأول لا يجب أن يزيد عن 100 حرف")]
        public string FirstName { get; set; } = "";

        [Required(ErrorMessage = "اسم العائلة مطلوب")]
        [StringLength(100, ErrorMessage = "اسم العائلة لا يجب أن يزيد عن 100 حرف")]
        public string LastName { get; set; } = "";

        [Required(ErrorMessage = "تاريخ الميلاد مطلوب")]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Required(ErrorMessage = "الجنس مطلوب")]
        public string Gender { get; set; } = "";

        [Required(ErrorMessage = "المحافظة مطلوبة")]
        [StringLength(100, ErrorMessage = "اسم المحافظة لا يجب أن يزيد عن 100 حرف")]
        public string City { get; set; } = "";

        [Required(ErrorMessage = "المنطقة مطلوبة")]
        [StringLength(100, ErrorMessage = "اسم المنطقة لا يجب أن يزيد عن 100 حرف")]
        public string District { get; set; } = "";

        [StringLength(255, ErrorMessage = "العنوان التفصيلي لا يجب أن يزيد عن 255 حرف")]
        public string? Location { get; set; }

        [EmailAddress(ErrorMessage = "البريد الإلكتروني غير صحيح")]
        [StringLength(255, ErrorMessage = "البريد الإلكتروني لا يجب أن يزيد عن 255 حرف")]
        public string? Email { get; set; }

        public IFormFile? ProfileImage { get; set; }

        [Required(ErrorMessage = "نوع الحساب مطلوب")]
        public UserType UserType { get; set; }

        // Store fields (for sellers)
        [StringLength(255, ErrorMessage = "اسم المتجر لا يجب أن يزيد عن 255 حرف")]
        public string? StoreName { get; set; }

        [StringLength(1000, ErrorMessage = "وصف المتجر لا يجب أن يزيد عن 1000 حرف")]
        public string? StoreDescription { get; set; }

        [Url(ErrorMessage = "رابط الموقع الأول غير صحيح")]
        public string? WebsiteUrl1 { get; set; }

        [Url(ErrorMessage = "رابط الموقع الثاني غير صحيح")]
        public string? WebsiteUrl2 { get; set; }

        [Url(ErrorMessage = "رابط الموقع الثالث غير صحيح")]
        public string? WebsiteUrl3 { get; set; }

        public List<int>? StoreCategories { get; set; }
    }
}