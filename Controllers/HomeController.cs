using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Data;

namespace OnlineShop.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;
        public HomeController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var featured = await _db.Products
                .Include(p => p.Category)
                .Where(p => p.IsFeatured && p.IsActive)
                .OrderByDescending(p => p.SoldCount)
                .Take(8).ToListAsync();

            var categories = await _db.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.SortOrder)
                .Take(8).ToListAsync();

            var bestSellers = await _db.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.SoldCount)
                .Take(4).ToListAsync();

            var newArrivals = await _db.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .Take(4).ToListAsync();

            ViewBag.FeaturedProducts = featured;
            ViewBag.Categories = categories;
            ViewBag.BestSellers = bestSellers;
            ViewBag.NewArrivals = newArrivals;
            return View();
        }
    }
}
