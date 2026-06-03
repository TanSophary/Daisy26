using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace OnlineShop.Models
{
    public class Coupon
    {
        [Key]
        public int CouponId { get; set; }
        [Required, MaxLength(50)]
        public string Code { get; set; } = "";
        public string DiscountType { get; set; } = "Percent";
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountValue { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal MinOrderAmt { get; set; }
        public int? UsageLimit { get; set; }
        public int UsedCount { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
