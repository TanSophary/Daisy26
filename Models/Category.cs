using System.ComponentModel.DataAnnotations;
namespace OnlineShop.Models
{
    public class Category
    {
        public int CategoryId { get; set; }
        [MaxLength(10)]
        public string CategoryCode { get; set; } = "";  // e.g. C01
        [Required, MaxLength(100)]
        public string Name { get; set; } = "";
        [MaxLength(500)]
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
