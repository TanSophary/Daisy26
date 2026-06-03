using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Data;
using OnlineShop.Models;
using OnlineShop.Services;

namespace OnlineShop.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;
        private readonly CodeGeneratorService _codes;
        public AdminController(ApplicationDbContext db, IConfiguration config, IWebHostEnvironment env, CodeGeneratorService codes)
        { _db = db; _config = config; _env = env; _codes = codes; }

        private async Task<string?> SaveUploadedFile(IFormFile? file, string subFolder)
        {
            if (file == null || file.Length == 0) return null;
            var folder = Path.Combine(_env.WebRootPath, "uploads", subFolder);
            Directory.CreateDirectory(folder);
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = Guid.NewGuid().ToString("N") + ext;
            var path = Path.Combine(folder, fileName);
            using var stream = new FileStream(path, FileMode.Create);
            await file.CopyToAsync(stream);
            return $"/uploads/{subFolder}/{fileName}";
        }

        private bool IsAdminLoggedIn() => HttpContext.Session.GetString("AdminLoggedIn") == "true";
        private IActionResult RequireAdmin() => RedirectToAction("Login", "AdminAuth");

        // ===================== DASHBOARD =====================
        public async Task<IActionResult> Dashboard()
        {
            if (!IsAdminLoggedIn()) return RequireAdmin();
            ViewData["Page"] = "Dashboard";
            ViewData["Title"] = "Dashboard";
            ViewBag.TotalProducts = await _db.Products.CountAsync();
            ViewBag.TotalOrders = await _db.Orders.CountAsync();
            ViewBag.TotalCustomers = await _db.Customers.CountAsync();
            ViewBag.TotalRevenue = await _db.Orders
                .Where(o => o.PaymentStatus == "Paid")
                .SumAsync(o => o.Total);
            ViewBag.RecentOrders = await _db.Orders
                .OrderByDescending(o => o.CreatedAt).Take(8).ToListAsync();
            ViewBag.LowStock = await _db.Products
                .Where(p => p.Stock < 5 && p.IsActive)
                .OrderBy(p => p.Stock).Take(6).ToListAsync();
            return View();
        }

        // ===================== PRODUCTS =====================
        public async Task<IActionResult> Products(string? search, int page = 1)
        {
            if (!IsAdminLoggedIn()) return RequireAdmin();
            ViewData["Page"] = "Products";
            ViewData["Title"] = "Manage Products";
            int size = 15;
            var q = _db.Products.Include(p => p.Category).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(p => p.Name.Contains(search) || p.ProductCode.Contains(search));
            q = q.OrderByDescending(p => p.CreatedAt);
            int total = await q.CountAsync();
            ViewBag.Products = await q.Skip((page - 1) * size).Take(size).ToListAsync();
            ViewBag.Search = search;
            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / size);
            ViewBag.Total = total;
            return View();
        }

        public async Task<IActionResult> CreateProduct()
        {
            if (!IsAdminLoggedIn()) return RequireAdmin();
            ViewData["Page"] = "Products";
            ViewData["Title"] = "Add New Product";
            ViewBag.Categories = await _db.Categories.Where(c => c.IsActive).ToListAsync();
            return View();
        }

        /// <summary>Returns preview of next ProductCode for a given categoryId (AJAX).</summary>
        [HttpGet]
        public async Task<IActionResult> PreviewProductCode(int categoryId)
        {
            var category = await _db.Categories.FindAsync(categoryId);
            if (category == null) return Json(new { code = "—" });
            var code = await _codes.NextProductCodeAsync(category.CategoryCode ?? "C00");
            return Json(new { code });
        }

        [HttpPost]
        public async Task<IActionResult> CreateProduct(Product model, IFormFile? mainImageFile, List<IFormFile>? extraImageFiles,
            List<string>? colorNames, List<int>? colorStocks)
        {
            ModelState.Remove("ImageUrl");
            ModelState.Remove("ProductCode");
            if (!ModelState.IsValid)
            {
                ViewData["Page"] = "Products";
                ViewData["Title"] = "Add New Product";
                ViewBag.Categories = await _db.Categories.Where(c => c.IsActive).ToListAsync();
                return View(model);
            }

            // Auto-generate ProductCode from category code
            var category = await _db.Categories.FindAsync(model.CategoryId);
            var catCode = category?.CategoryCode ?? "C00";
            model.ProductCode = await _codes.NextProductCodeAsync(catCode);

            // Save main image file
            if (mainImageFile != null && mainImageFile.Length > 0)
                model.ImageUrl = await SaveUploadedFile(mainImageFile, "products");

            // Build Colors string and compute total Stock from color stocks
            if (colorNames != null && colorNames.Any(c => !string.IsNullOrWhiteSpace(c)))
            {
                var validColors = colorNames
                    .Select((c, i) => new { Name = c?.Trim(), Stock = colorStocks != null && i < colorStocks.Count ? colorStocks[i] : 0 })
                    .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                    .ToList();
                model.Colors = string.Join(",", validColors.Select(x => x.Name));
                model.Stock = validColors.Sum(x => x.Stock);
            }

            model.CreatedAt = DateTime.Now;
            model.UpdatedAt = DateTime.Now;
            _db.Products.Add(model);
            await _db.SaveChangesAsync();

            // Save color stocks
            if (colorNames != null)
            {
                for (int i = 0; i < colorNames.Count; i++)
                {
                    var name = colorNames[i]?.Trim();
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    var stock = colorStocks != null && i < colorStocks.Count ? colorStocks[i] : 0;
                    _db.ProductColorStocks.Add(new ProductColorStock { ProductId = model.ProductId, Color = name!, Stock = stock });
                }
                await _db.SaveChangesAsync();
            }

            // Save additional images
            if (extraImageFiles != null)
            {
                int sort = 1;
                foreach (var file in extraImageFiles.Where(f => f != null && f.Length > 0))
                {
                    var url = await SaveUploadedFile(file, "products");
                    if (url != null)
                        _db.ProductImages.Add(new ProductImage { ProductId = model.ProductId, ImageUrl = url, SortOrder = sort++ });
                }
                await _db.SaveChangesAsync();
            }

            TempData["Success"] = $"Product '{model.Name}' created successfully!";
            return RedirectToAction("Products");
        }

        public async Task<IActionResult> EditProduct(int id)
        {
            if (!IsAdminLoggedIn()) return RequireAdmin();
            ViewData["Page"] = "Products";
            ViewData["Title"] = "Edit Product";
            var product = await _db.Products
                .Include(p => p.ProductImages)
                .Include(p => p.ColorStocks)
                .FirstOrDefaultAsync(p => p.ProductId == id);
            if (product == null) return NotFound();
            ViewBag.Categories = await _db.Categories.Where(c => c.IsActive).ToListAsync();
            ViewBag.ProductImages = product.ProductImages.OrderBy(i => i.SortOrder).ToList();
            ViewBag.ColorStocks = product.ColorStocks.ToList();
            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> HardDeleteProduct(int id)
        {
            var product = await _db.Products
                .Include(p => p.ProductImages)
                .Include(p => p.ColorStocks)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product != null)
            {
                // Remove related data first
                _db.ProductImages.RemoveRange(product.ProductImages);
                _db.ProductColorStocks.RemoveRange(product.ColorStocks);

                _db.Products.Remove(product);
                await _db.SaveChangesAsync();

                TempData["Success"] = $"Product '{product.Name}' permanently deleted.";
            }

            return RedirectToAction("Products");
        }

        [HttpPost]
        public async Task<IActionResult> EditProduct(int id, Product model, IFormFile? mainImageFile, List<IFormFile>? extraImageFiles, List<int?>? deleteImageIds,
            List<string>? colorNames, List<int>? colorStocks)
        {
            ModelState.Remove("ImageUrl");
            ModelState.Remove("ProductCode");
            if (!ModelState.IsValid)
            {
                ViewData["Page"] = "Products";
                ViewData["Title"] = "Edit Product";
                ViewBag.Categories = await _db.Categories.Where(c => c.IsActive).ToListAsync();
                ViewBag.ProductImages = await _db.ProductImages.Where(i => i.ProductId == id).OrderBy(i => i.SortOrder).ToListAsync();
                ViewBag.ColorStocks = await _db.ProductColorStocks.Where(cs => cs.ProductId == id).ToListAsync();
                return View(model);
            }
            var product = await _db.Products
                .Include(p => p.ProductImages)
                .Include(p => p.ColorStocks)
                .FirstOrDefaultAsync(p => p.ProductId == id);
            if (product == null) return NotFound();
            product.CategoryId = model.CategoryId;
            product.Name = model.Name;
            product.Description = model.Description;
            product.Price = model.Price;
            product.OldPrice = model.OldPrice;
            // ProductCode is auto-generated and never changed on edit
            product.IsFeatured = model.IsFeatured;
            product.IsActive = model.IsActive;
            product.Rating = model.Rating;
            product.UpdatedAt = DateTime.Now;
            product.Description = model.Description;

            // Update color stocks and recompute total stock
            if (colorNames != null && colorNames.Any(c => !string.IsNullOrWhiteSpace(c)))
            {
                var validColors = colorNames
                    .Select((c, i) => new { Name = c?.Trim(), Stock = colorStocks != null && i < colorStocks.Count ? colorStocks[i] : 0 })
                    .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                    .ToList();

                product.Colors = string.Join(",", validColors.Select(x => x.Name));
                product.Stock = validColors.Sum(x => x.Stock);

                // Remove old color stocks and replace
                // Remove old color stocks
                var oldStocks = await _db.ProductColorStocks
                    .Where(cs => cs.ProductId == id)
                    .ToListAsync();

                _db.ProductColorStocks.RemoveRange(oldStocks);
                await _db.SaveChangesAsync(); // IMPORTANT

                // Add new color stocks
                foreach (var vc in validColors)
                {
                    _db.ProductColorStocks.Add(new ProductColorStock
                    {
                        ProductId = id,
                        Color = vc.Name!,
                        Stock = vc.Stock
                    });
                }
            }
            else
            {
                // No colors — keep manual stock value
                product.Colors = model.Colors;
                product.Stock = model.Stock;
            }

            // Replace main image if new file uploaded
            if (mainImageFile != null && mainImageFile.Length > 0)
                product.ImageUrl = await SaveUploadedFile(mainImageFile, "products");

            // Delete images marked for removal
            var removeIds = (deleteImageIds ?? new List<int?>()).Where(i => i.HasValue).Select(i => i!.Value).ToHashSet();
            var toRemove = product.ProductImages.Where(i => removeIds.Contains(i.ImageId)).ToList();
            _db.ProductImages.RemoveRange(toRemove);

            // Add new extra images
            if (extraImageFiles != null)
            {
                int sort = (product.ProductImages.Count - toRemove.Count) + 1;
                foreach (var file in extraImageFiles.Where(f => f != null && f.Length > 0))
                {
                    var url = await SaveUploadedFile(file, "products");
                    if (url != null)
                        _db.ProductImages.Add(new ProductImage { ProductId = id, ImageUrl = url, SortOrder = sort++ });
                }
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = $"Product '{product.Name}' updated successfully!";
            return RedirectToAction("Products");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product != null)
            {
                product.IsActive = false; // soft delete
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Product '{product.Name}' deleted.";
            }
            return RedirectToAction("Products");
        }

        // ===================== CATEGORIES =====================
        public async Task<IActionResult> Categories()
        {
            if (!IsAdminLoggedIn()) return RequireAdmin();
            ViewData["Page"] = "Categories";
            ViewData["Title"] = "Manage Categories";
            var cats = await _db.Categories
                .Include(c => c.Products)
                .OrderBy(c => c.SortOrder).ToListAsync();
            return View(cats);
        }

        public IActionResult CreateCategory()
        {
            if (!IsAdminLoggedIn()) return RequireAdmin();
            ViewData["Page"] = "Categories";
            ViewData["Title"] = "Add New Category";
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateCategory(Category model)
        {
            if (!ModelState.IsValid) { ViewData["Page"] = "Categories"; ViewData["Title"] = "Add New Category"; return View(model); }
            model.CategoryCode = await _codes.NextCategoryCodeAsync();
            model.CreatedAt = DateTime.Now;
            _db.Categories.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Category '{model.Name}' created! Code: {model.CategoryCode}";
            return RedirectToAction("Categories");
        }

        public async Task<IActionResult> EditCategory(int id)
        {
            if (!IsAdminLoggedIn()) return RequireAdmin();
            ViewData["Page"] = "Categories";
            ViewData["Title"] = "Edit Category";
            var cat = await _db.Categories.FindAsync(id);
            if (cat == null) return NotFound();
            return View(cat);
        }

        [HttpPost]
        public async Task<IActionResult> EditCategory(int id, Category model)
        {
            if (!ModelState.IsValid) { ViewData["Page"] = "Categories"; ViewData["Title"] = "Edit Category"; return View(model); }
            var cat = await _db.Categories.FindAsync(id);
            if (cat == null) return NotFound();
            cat.Name = model.Name;
            cat.Description = model.Description;
            cat.ImageUrl = model.ImageUrl;
            cat.IsActive = model.IsActive;
            cat.SortOrder = model.SortOrder;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Category '{cat.Name}' updated!";
            return RedirectToAction("Categories");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var cat = await _db.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.CategoryId == id);
            if (cat != null)
            {
                if (cat.Products.Any())
                { TempData["Error"] = "Cannot delete category with existing products."; return RedirectToAction("Categories"); }
                _db.Categories.Remove(cat);
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Category '{cat.Name}' deleted.";
            }
            return RedirectToAction("Categories");
        }
        public async Task<IActionResult> Orders(string? status, int page = 1)
        {
            if (!IsAdminLoggedIn()) return RequireAdmin();

            int size = 15;
            var q = _db.Orders.AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                q = q.Where(o => o.Status == status);

            q = q.OrderByDescending(o => o.CreatedAt);

            int total = await q.CountAsync();
            var orders = await q.Skip((page - 1) * size).Take(size).ToListAsync();

            // Use ViewData - more reliable than ViewBag
            ViewData["Orders"] = orders;
            ViewData["Status"] = status;
            ViewData["CurrentPage"] = page;                    // renamed for clarity
            ViewData["TotalPages"] = (int)Math.Ceiling((double)total / size);

            return View();
        }

        public async Task<IActionResult> OrderDetail(int id)
        {
            if (!IsAdminLoggedIn()) return RequireAdmin();
            ViewData["Page"] = "Orders";
            ViewData["Title"] = "Order Detail";
            var order = await _db.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == id);
            if (order == null) return NotFound();
            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(int id, string status, string paymentStatus)
        {
            var order = await _db.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == id);
            if (order != null)
            {
                var oldStatus = order.Status;
                order.Status = status;
                // For COD orders: keep PaymentStatus as Unpaid unless admin explicitly marks Paid
                // Admin can freely change order status regardless of payment status for COD
                order.PaymentStatus = paymentStatus;
                order.UpdatedAt = DateTime.Now;
                await _db.SaveChangesAsync();
                TempData["Success"] = "Order status updated!";

                // Notify customer via Telegram if status changed
                if (oldStatus != status && order.Customer?.TelegramChatId != null)
                {
                    var botToken = _config["Telegram:BotToken"] ?? "";
                    var emoji = status switch
                    {
                        "Processing" => "⚙️",
                        "Shipped" => "🚚",
                        "Delivered" => "🎉",
                        "Cancelled" => "❌",
                        _ => "📋"
                    };
                    var msg = $"{emoji} <b>Order Update - Daisy26 Shopping</b>\n\n" +
                              $"📋 <b>Order:</b> {order.OrderNumber}\n" +
                              $"📦 <b>Status:</b> {status}\n" +
                              $"💳 <b>Payment:</b> {paymentStatus}\n\n" +
                              $"Thank you for shopping with us! 🌸\n" +
                              $"Contact us: @Pha_Rie | 0979534329";
                    _ = CartController.SendTelegramMessage(botToken, order.Customer.TelegramChatId, msg);
                }
            }
            return RedirectToAction("OrderDetail", new { id });
        }

        // ===================== TELEGRAM TEST + WEBHOOK SETUP =====================
        public async Task<IActionResult> TelegramTest()
        {
            var botToken = _config["Telegram:BotToken"] ?? "";
            var groupChat = _config["Telegram:GroupChatId"] ?? "";
            var webhookUrl = _config["Telegram:WebhookUrl"] ?? "";
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<pre style='font-size:13px;background:#111;color:#0f0;padding:24px;border-radius:8px;max-width:900px;margin:30px auto'>");
            sb.AppendLine("=== Telegram Full Debug ===\n");
            sb.AppendLine("BotToken    : " + botToken);
            sb.AppendLine("GroupChatId : " + groupChat);
            sb.AppendLine("WebhookUrl  : " + webhookUrl + "\n");
            if (string.IsNullOrEmpty(botToken)) { sb.AppendLine("FAIL: BotToken empty"); return Content(sb.ToString() + "</pre>", "text/html"); }

            // 1. getMe
            try
            {
                using var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                var r = await http.GetAsync("https://api.telegram.org/bot" + botToken + "/getMe");
                sb.AppendLine("[getMe] " + (int)r.StatusCode + ": " + await r.Content.ReadAsStringAsync());
            }
            catch (Exception ex) { sb.AppendLine("[getMe] EXCEPTION: " + ex.Message); }

            // 2. getWebhookInfo — is webhook registered?
            try
            {
                using var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                var r = await http.GetAsync("https://api.telegram.org/bot" + botToken + "/getWebhookInfo");
                sb.AppendLine("\n[getWebhookInfo] " + (int)r.StatusCode + ": " + await r.Content.ReadAsStringAsync());
            }
            catch (Exception ex) { sb.AppendLine("[getWebhookInfo] EXCEPTION: " + ex.Message); }

            // 3. setWebhook if WebhookUrl configured
            if (!string.IsNullOrEmpty(webhookUrl))
            {
                try
                {
                    using var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                    var data = new System.Net.Http.FormUrlEncodedContent(new[] {
                        new KeyValuePair<string,string>("url", webhookUrl),
                        new KeyValuePair<string,string>("allowed_updates", "[\"message\"]"),
                    });
                    var r = await http.PostAsync("https://api.telegram.org/bot" + botToken + "/setWebhook", data);
                    sb.AppendLine("\n[setWebhook] " + (int)r.StatusCode + ": " + await r.Content.ReadAsStringAsync());
                }
                catch (Exception ex) { sb.AppendLine("[setWebhook] EXCEPTION: " + ex.Message); }
            }
            else
            {
                sb.AppendLine("\nWARNING: Telegram:WebhookUrl not set in appsettings.json");
                sb.AppendLine("Add it like: \"WebhookUrl\": \"https://YOUR-DOMAIN/Cart/TelegramWebhook\"");
                sb.AppendLine("If running locally, use ngrok: ngrok http 5000  then use the https URL");
            }

            // 4. sendMessage to group
            if (!string.IsNullOrEmpty(groupChat))
            {
                try
                {
                    using var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                    var data = new System.Net.Http.FormUrlEncodedContent(new[] {
                        new KeyValuePair<string,string>("chat_id", groupChat),
                        new KeyValuePair<string,string>("text", "🧪 TEST: Group alert is working!"),
                        new KeyValuePair<string,string>("parse_mode", "HTML"),
                    });
                    var r = await http.PostAsync("https://api.telegram.org/bot" + botToken + "/sendMessage", data);
                    sb.AppendLine("\n[sendMessage group] " + (int)r.StatusCode + ": " + await r.Content.ReadAsStringAsync());
                }
                catch (Exception ex) { sb.AppendLine("[sendMessage group] EXCEPTION: " + ex.Message); }
            }

            sb.AppendLine("\n=== Done ===");
            sb.AppendLine("</pre>");
            return Content(sb.ToString(), "text/html");
        }

        public async Task<IActionResult> SetWebhook()
        {
            var botToken = _config["Telegram:BotToken"] ?? "";
            var webhookUrl = _config["Telegram:WebhookUrl"] ?? "";
            if (string.IsNullOrEmpty(botToken) || string.IsNullOrEmpty(webhookUrl))
                return Content("Error: BotToken or WebhookUrl missing in appsettings.json");
            using var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var data = new System.Net.Http.FormUrlEncodedContent(new[] {
                new KeyValuePair<string,string>("url", webhookUrl),
                new KeyValuePair<string,string>("allowed_updates", "[\"message\"]"),
            });
            var r = await http.PostAsync("https://api.telegram.org/bot" + botToken + "/setWebhook", data);
            var body = await r.Content.ReadAsStringAsync();
            TempData["Success"] = "Webhook set: " + body;
            return RedirectToAction("TelegramTest");
        }

        // ===================== CUSTOMERS =====================
        public async Task<IActionResult> Customers(string? search, int page = 1)
        {
            if (!IsAdminLoggedIn()) return RequireAdmin();
            ViewData["Page"] = "Customers";
            ViewData["Title"] = "Manage Customers";
            int size = 15;
            var q = _db.Customers.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(c => c.FullName.Contains(search) || c.Email.Contains(search));
            q = q.OrderByDescending(c => c.CreatedAt);
            int total = await q.CountAsync();
            ViewBag.Customers = await q.Skip((page - 1) * size).Take(size).ToListAsync();
            ViewBag.Search = search;
            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / size);
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ToggleCustomer(int id)
        {
            var c = await _db.Customers.FindAsync(id);
            if (c != null) { c.IsActive = !c.IsActive; await _db.SaveChangesAsync(); }
            TempData["Success"] = "Customer status updated.";
            return RedirectToAction("Customers");
        }

        // ===================== COUPONS =====================
        public async Task<IActionResult> Coupons()
        {
            if (!IsAdminLoggedIn()) return RequireAdmin();
            ViewData["Page"] = "Coupons";
            ViewData["Title"] = "Manage Coupons";
            var coupons = await _db.Coupons.OrderByDescending(c => c.CouponId).ToListAsync();
            return View(coupons);
        }

        public IActionResult CreateCoupon()
        {
            if (!IsAdminLoggedIn()) return RequireAdmin();
            ViewData["Page"] = "Coupons";
            ViewData["Title"] = "Add New Coupon";
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateCoupon(Coupon model)
        {
            if (!ModelState.IsValid) { ViewData["Page"] = "Coupons"; ViewData["Title"] = "Add New Coupon"; return View(model); }
            _db.Coupons.Add(model);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Coupon '{model.Code}' created!";
            return RedirectToAction("Coupons");
        }

        public async Task<IActionResult> EditCoupon(int id)
        {
            if (!IsAdminLoggedIn()) return RequireAdmin();
            ViewData["Page"] = "Coupons";
            ViewData["Title"] = "Edit Coupon";
            var cp = await _db.Coupons.FindAsync(id);
            if (cp == null) return NotFound();
            return View(cp);
        }

        [HttpPost]
        public async Task<IActionResult> EditCoupon(int id, Coupon model)
        {
            if (!ModelState.IsValid) { ViewData["Page"] = "Coupons"; ViewData["Title"] = "Edit Coupon"; return View(model); }
            var cp = await _db.Coupons.FindAsync(id);
            if (cp == null) return NotFound();
            cp.Code = model.Code;
            cp.DiscountType = model.DiscountType;
            cp.DiscountValue = model.DiscountValue;
            cp.MinOrderAmt = model.MinOrderAmt;
            cp.UsageLimit = model.UsageLimit;
            cp.ExpiryDate = model.ExpiryDate;
            cp.IsActive = model.IsActive;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Coupon updated!";
            return RedirectToAction("Coupons");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCoupon(int id)
        {
            var cp = await _db.Coupons.FindAsync(id);
            if (cp != null) { _db.Coupons.Remove(cp); await _db.SaveChangesAsync(); TempData["Success"] = "Coupon deleted."; }
            return RedirectToAction("Coupons");
        }

        // ===================== REVIEWS =====================
        public async Task<IActionResult> Reviews(int page = 1)
        {
            if (!IsAdminLoggedIn()) return RequireAdmin();
            ViewData["Page"] = "Reviews";
            ViewData["Title"] = "Manage Reviews";
            int size = 15;
            var q = _db.Reviews.Include(r => r.Product)
                       .OrderByDescending(r => r.CreatedAt);
            int total = await q.CountAsync();
            ViewBag.Reviews = await q.Skip((page - 1) * size).Take(size).ToListAsync();
            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / size);
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ApproveReview(int id)
        {
            var r = await _db.Reviews.FindAsync(id);
            if (r != null) { r.IsApproved = true; await _db.SaveChangesAsync(); TempData["Success"] = "Review approved."; }
            return RedirectToAction("Reviews");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var r = await _db.Reviews.FindAsync(id);
            if (r != null) { _db.Reviews.Remove(r); await _db.SaveChangesAsync(); TempData["Success"] = "Review deleted."; }
            return RedirectToAction("Reviews");
        }
    }
}