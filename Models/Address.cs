using System.ComponentModel.DataAnnotations;
namespace OnlineShop.Models
{
    public class Address
    {
        [Key]
        public int AddressId { get; set; }
        public int CustomerId { get; set; }
        [Required, MaxLength(150)]
        public string FullName { get; set; } = "";
        [Required, MaxLength(20)]
        public string Phone { get; set; } = "";
        [Required, MaxLength(300)]
        public string Street { get; set; } = "";
        [Required, MaxLength(100)]
        public string City { get; set; } = "";
        [Required, MaxLength(100)]
        public string Province { get; set; } = "";
        [MaxLength(100)]
        public string Country { get; set; } = "Cambodia";
        public bool IsDefault { get; set; }
        public Customer? Customer { get; set; }
    }
}
