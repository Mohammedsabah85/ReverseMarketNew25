// Models/Category.cs
using System.ComponentModel.DataAnnotations;

namespace ReverseMarket.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual ICollection<SubCategory1> SubCategories1 { get; set; } = new List<SubCategory1>();
        public virtual ICollection<Request> Requests { get; set; } = new List<Request>();
    }

    public class SubCategory1
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public int CategoryId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual Category Category { get; set; }
        public virtual ICollection<SubCategory2> SubCategories2 { get; set; } = new List<SubCategory2>();
        public virtual ICollection<Request> Requests { get; set; } = new List<Request>();
    }

    public class SubCategory2
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public int SubCategory1Id { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual SubCategory1 SubCategory1 { get; set; }
        public virtual ICollection<Request> Requests { get; set; } = new List<Request>();
    }
}