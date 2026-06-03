using System.ComponentModel.DataAnnotations;
namespace OnlineShop.Models
{
    public class ChatMessage
    {
        public int ChatMessageId { get; set; }
        public string SessionId { get; set; } = "";
        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
        public string Message { get; set; } = "";
        public bool IsFromAdmin { get; set; } = false;
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}