using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace OnlineShop.Models
{
    public class OrderItem
    {
        [Key]
        public int OrderItemId { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        [Required, MaxLength(200)]
        public string ProductName { get; set; } = "";
        public string? ProductImage { get; set; }
        [MaxLength(100)]
        public string? Color { get; set; }
        public int Quantity { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }
        public Order? Order { get; set; }
        public Product? Product { get; set; }
    }
}
