using System.ComponentModel.DataAnnotations;
namespace OnlineShop.Models
{
    public class Review
    {
        [Key]
        public int ReviewId { get; set; }
        public int ProductId { get; set; }
        public int? CustomerId { get; set; }
        [Required, MaxLength(100)]
        public string ReviewerName { get; set; } = "";
        [Range(1, 5)]
        public int Rating { get; set; }
        [MaxLength(1000)]
        public string? Comment { get; set; }
        public bool IsApproved { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public Product? Product { get; set; }
    }
}
