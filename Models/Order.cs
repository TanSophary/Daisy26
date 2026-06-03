using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace OnlineShop.Models
{
    public class Order
    {
        public int OrderId { get; set; }
        public int? CustomerId { get; set; }
        [Required, MaxLength(30)]
        public string OrderNumber { get; set; } = "";
        public string Status { get; set; } = "Pending";
        public string PaymentMethod { get; set; } = "Cash";
        public string PaymentStatus { get; set; } = "Unpaid";
        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal Discount { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingFee { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }
        public string? CouponCode { get; set; }
        [Required, MaxLength(150)]
        public string ShipName { get; set; } = "";
        [Required, MaxLength(20)]
        public string ShipPhone { get; set; } = "";
        [Required, MaxLength(400)]
        public string ShipAddress { get; set; } = "";
        [MaxLength(500)]
        public string? Note { get; set; }
        // Map location (lat/lng set during checkout)
        public double? LocationLat { get; set; }
        public double? LocationLng { get; set; }
        [MaxLength(500)]
        public string? InvoicePath { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public Customer? Customer { get; set; }
        [MaxLength(100)]
        public string? DeliveryMethod { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
