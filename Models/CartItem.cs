using System.ComponentModel.DataAnnotations;
namespace OnlineShop.Models
{
    public class CartItem
    {
        [Key]
        public int CartItemId { get; set; }
        public string SessionId { get; set; } = "";
        public int? CustomerId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; } = 1;
        [MaxLength(50)]
        public string? Color { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public Product? Product { get; set; }
    }
}
