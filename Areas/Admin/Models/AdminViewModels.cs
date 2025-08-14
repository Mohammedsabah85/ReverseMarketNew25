using ReverseMarket.Models;
using System.ComponentModel.DataAnnotations;


namespace ReverseMarket.Areas.Admin.Models
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalRequests { get; set; }
        public int PendingRequests { get; set; }
        public int TotalStores { get; set; }
        public int TotalCategories { get; set; }
        public List<Request> RecentRequests { get; set; } = new();
    }

    public class AdminRequestsViewModel
    {
        public List<Request> Requests { get; set; } = new();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public RequestStatus? StatusFilter { get; set; }
    }

    public class CreateAdvertisementViewModel
    {
        [Required(ErrorMessage = "عنوان الإعلان مطلوب")]
        [StringLength(255, ErrorMessage = "عنوان الإعلان لا يجب أن يزيد عن 255 حرف")]
        public string Title { get; set; } = "";

        [StringLength(1000, ErrorMessage = "وصف الإعلان لا يجب أن يزيد عن 1000 حرف")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "صورة الإعلان مطلوبة")]
        public IFormFile Image { get; set; } = null!;

        [Url(ErrorMessage = "رابط الإعلان غير صحيح")]
        public string? LinkUrl { get; set; }

        [Required(ErrorMessage = "نوع الإعلان مطلوب")]
        public AdvertisementType Type { get; set; }

        [Range(0, 999, ErrorMessage = "ترتيب العرض يجب أن يكون بين 0 و 999")]
        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;

        [Required(ErrorMessage = "تاريخ البداية مطلوب")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Now;

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }
    }
}