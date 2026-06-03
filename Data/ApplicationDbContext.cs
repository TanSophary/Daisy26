using Microsoft.EntityFrameworkCore;
using OnlineShop.Models;

namespace OnlineShop.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<Coupon> Coupons { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<ProductColorStock> ProductColorStocks { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<Wishlist> Wishlists { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Explicitly define primary keys for non-standard names
            modelBuilder.Entity<ProductImage>().HasKey(e => e.ImageId);
            modelBuilder.Entity<Review>().HasKey(e => e.ReviewId);
            modelBuilder.Entity<CartItem>().HasKey(e => e.CartItemId);
            modelBuilder.Entity<OrderItem>().HasKey(e => e.OrderItemId);
            modelBuilder.Entity<Address>().HasKey(e => e.AddressId);
            modelBuilder.Entity<Coupon>().HasKey(e => e.CouponId);

            // Category
            modelBuilder.Entity<Category>(e => {
                e.HasKey(c => c.CategoryId);
            });

            // Product
            modelBuilder.Entity<Product>(e => {
                e.HasKey(p => p.ProductId);
                e.HasIndex(p => p.ProductCode).IsUnique();
                e.HasOne(p => p.Category)
                 .WithMany(c => c.Products)
                 .HasForeignKey(p => p.CategoryId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // Customer
            modelBuilder.Entity<Customer>(e => {
                e.HasKey(c => c.CustomerId);
                e.HasIndex(c => c.Email).IsUnique();
            });

            // Order
            modelBuilder.Entity<Order>(e => {
                e.HasKey(o => o.OrderId);
                e.HasIndex(o => o.OrderNumber).IsUnique();
            });

            // OrderItem relationships
            modelBuilder.Entity<OrderItem>(e => {
                e.HasOne(oi => oi.Order)
                 .WithMany(o => o.OrderItems)
                 .HasForeignKey(oi => oi.OrderId)
                 .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(oi => oi.Product)
                 .WithMany(p => p.OrderItems)
                 .HasForeignKey(oi => oi.ProductId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ProductImage
            modelBuilder.Entity<ProductImage>(e => {
                e.HasOne(pi => pi.Product)
                 .WithMany(p => p.ProductImages)
                 .HasForeignKey(pi => pi.ProductId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // Review
            modelBuilder.Entity<Review>(e => {
                e.HasOne(r => r.Product)
                 .WithMany(p => p.Reviews)
                 .HasForeignKey(r => r.ProductId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // CartItem index
            modelBuilder.Entity<CartItem>(e => {
                e.HasIndex(c => c.SessionId);
            });

            // Wishlist
            modelBuilder.Entity<Wishlist>(e => {
                e.HasKey(w => w.WishlistId);
                e.HasOne(w => w.Product)
                 .WithMany()
                 .HasForeignKey(w => w.ProductId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // ProductColorStock
            modelBuilder.Entity<ProductColorStock>(e => {
                e.HasOne(cs => cs.Product)
                 .WithMany(p => p.ColorStocks)
                 .HasForeignKey(cs => cs.ProductId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            // Address
            modelBuilder.Entity<Address>(e => {
                e.HasOne(a => a.Customer)
                 .WithMany(c => c.Addresses)
                 .HasForeignKey(a => a.CustomerId)
                 .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}