using System.ComponentModel.DataAnnotations;
namespace OnlineShop.Models
{
    public class Customer
    {
        public int CustomerId { get; set; }
        [MaxLength(15)]
        public string CardCode { get; set; } = "";  // e.g. C-26-00001
        [Required, MaxLength(150)]
        public string FullName { get; set; } = "";
        [Required, MaxLength(200), EmailAddress]
        public string Email { get; set; } = "";
        [MaxLength(20)]
        public string? Phone { get; set; }
        public string PasswordHash { get; set; } = "";
        public string? AvatarUrl { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        // Telegram bot chat ID (set when customer starts the bot)
        [MaxLength(50)]
        public string? TelegramChatId { get; set; }
        // Password reset (token-based, legacy)
        [MaxLength(200)]
        public string? PasswordResetToken { get; set; }
        public DateTime? PasswordResetExpiry { get; set; }
        // OTP-based password reset
        [MaxLength(6)]
        public string? OtpCode { get; set; }
        public DateTime? OtpExpiry { get; set; }
        public ICollection<Address> Addresses { get; set; } = new List<Address>();
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
