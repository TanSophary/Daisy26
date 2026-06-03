using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Data;

namespace OnlineShop.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _db;
        public ProductsController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index(int? categoryId, string? search,
            string sort = "popular", int page = 1)
        {
            int pageSize = 12;
            var query = _db.Products.Include(p => p.Category)
                           .Where(p => p.IsActive);

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.Name.Contains(search) ||
                                         p.Description!.Contains(search));

            query = sort switch {
                "price-asc"  => query.OrderBy(p => p.Price),
                "price-desc" => query.OrderByDescending(p => p.Price),
                "newest"     => query.OrderByDescending(p => p.CreatedAt),
                "rating"     => query.OrderByDescending(p => p.Rating),
                _            => query.OrderByDescending(p => p.SoldCount)
            };

            int total = await query.CountAsync();
            var products = await query.Skip((page - 1) * pageSize)
                                      .Take(pageSize).ToListAsync();

            ViewBag.Categories = await _db.Categories.Where(c => c.IsActive)
                                          .OrderBy(c => c.SortOrder).ToListAsync();
            ViewBag.CurrentCategory = categoryId;
            ViewBag.Search = search;
            ViewBag.Sort = sort;
            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);
            ViewBag.Total = total;

            return View(products);
        }

        public async Task<IActionResult> Detail(int id)
        {
            var product = await _db.Products
                .Include(p => p.Category)
                .Include(p => p.Reviews.Where(r => r.IsApproved))
                .Include(p => p.ProductImages)
                .Include(p => p.ColorStocks)
                .FirstOrDefaultAsync(p => p.ProductId == id && p.IsActive);

            if (product == null) return NotFound();

            var related = await _db.Products
                .Include(p => p.Category)
                .Where(p => p.CategoryId == product.CategoryId &&
                            p.ProductId != id && p.IsActive)
                .Take(4).ToListAsync();

            ViewBag.Related = related;
            // Build color→stock dictionary for JS
            ViewBag.ColorStockMap = product.ColorStocks
                .ToDictionary(cs => cs.Color, cs => cs.Stock);
            return View(product);
        }
    }
}
