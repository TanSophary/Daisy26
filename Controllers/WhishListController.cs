using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Data;
using OnlineShop.Models;

namespace OnlineShop.Controllers
{
    public class WishlistController : Controller
    {
        private readonly ApplicationDbContext _db;
        public WishlistController(ApplicationDbContext db) => _db = db;

        private string GetSession()
        {
            var sid = HttpContext.Session.GetString("CartSession");
            if (sid == null) { sid = Guid.NewGuid().ToString(); HttpContext.Session.SetString("CartSession", sid); }
            return sid;
        }

        public async Task<IActionResult> Index()
        {
            var sid = GetSession();
            var items = await _db.Wishlists
                .Include(w => w.Product).ThenInclude(p => p!.Category)
                .Where(w => w.SessionId == sid)
                .ToListAsync();
            return View(items);
        }

        [HttpPost]
        public async Task<IActionResult> Toggle(int productId)
        {
            var sid = GetSession();
            var existing = await _db.Wishlists.FirstOrDefaultAsync(w => w.SessionId == sid && w.ProductId == productId);
            if (existing != null)
            {
                _db.Wishlists.Remove(existing);
                await _db.SaveChangesAsync();
                return Json(new { added = false });
            }
            _db.Wishlists.Add(new Wishlist { SessionId = sid, ProductId = productId });
            await _db.SaveChangesAsync();
            return Json(new { added = true });
        }

        [HttpPost]
        public async Task<IActionResult> Remove(int wishlistId)
        {
            var item = await _db.Wishlists.FindAsync(wishlistId);
            if (item != null) { _db.Wishlists.Remove(item); await _db.SaveChangesAsync(); }
            return RedirectToAction("Index");
        }
    }
}