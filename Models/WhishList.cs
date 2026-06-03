namespace OnlineShop.Models
{
    public class Wishlist
    {
        public int WishlistId { get; set; }
        public string SessionId { get; set; } = "";
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.Now;
    }
}