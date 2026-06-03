using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineShop.Models
{
    public class ProductColorStock
    {
        [Key]
        public int Id { get; set; }

        public int ProductId { get; set; }

        [Required, MaxLength(50)]
        public string Color { get; set; } = "";

        public int Stock { get; set; } = 0;

        public Product? Product { get; set; }
    }
}
