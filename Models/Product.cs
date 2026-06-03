using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace OnlineShop.Models
{
    public class Product
    {
        public int ProductId { get; set; }
        public int CategoryId { get; set; }
        [Required, MaxLength(200)]
        public string Name { get; set; } = "";
        [MaxLength(2000)]
        public string? Description { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal? OldPrice { get; set; }
        public int Stock { get; set; }
        [Required, MaxLength(20)]
        public string ProductCode { get; set; } = ""; 
        public string? ImageUrl { get; set; }
        [MaxLength(500)]
        public string? Colors { get; set; } 
        public bool IsFeatured { get; set; }
        public bool IsActive { get; set; } = true;
        [Column(TypeName = "decimal(3,2)")]
        public decimal Rating { get; set; }
        public int SoldCount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public Category? Category { get; set; }
        public ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public ICollection<ProductColorStock> ColorStocks { get; set; } = new List<ProductColorStock>();
        [NotMapped]
        public bool IsOnSale => OldPrice.HasValue && OldPrice > Price;
        [NotMapped]
        public int DiscountPercent => IsOnSale ? (int)Math.Round((1 - Price / OldPrice!.Value) * 100) : 0;
    }
}
