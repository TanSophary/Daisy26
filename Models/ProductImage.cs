using System.ComponentModel.DataAnnotations;
namespace OnlineShop.Models
{
    public class ProductImage
    {
        [Key]
        public int ImageId { get; set; }
        public int ProductId { get; set; }
        public string ImageUrl { get; set; } = "";
        public int SortOrder { get; set; }
        public Product? Product { get; set; }
    }
}
