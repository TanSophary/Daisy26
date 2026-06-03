using Microsoft.AspNetCore.Mvc;
using OnlineShop.Services;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Data;
using OnlineShop.Models;
using System.Collections.Generic;

namespace OnlineShop.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<CartController> _logger;

        public CartController(ApplicationDbContext db, IConfiguration config,
            IHttpClientFactory httpClientFactory, ILogger<CartController> logger)
        {
            _db = db;
            _config = config;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        private string GetSession() =>
            HttpContext.Session.GetString("CartSession") ?? CreateSession();

        private string CreateSession()
        {
            var id = Guid.NewGuid().ToString();
            HttpContext.Session.SetString("CartSession", id);
            return id;
        }

        public async Task<IActionResult> Index()
        {
            var sid = GetSession();
            var items = await _db.CartItems
                .Include(c => c.Product).ThenInclude(p => p!.Category)
                .Where(c => c.SessionId == sid)
                .ToListAsync();
            return View(items);
        }

        [HttpPost]
        public async Task<IActionResult> Add(int productId, int qty = 1, string? color = null)
        {
            var sid = GetSession();

            // Check stock — per color if color stocks exist, otherwise total product stock
            var product = await _db.Products.FindAsync(productId);
            if (product == null) { TempData["Error"] = "Product not found."; return RedirectToAction("Index"); }

            int availableStock = product.Stock;
            if (!string.IsNullOrWhiteSpace(color))
            {
                var colorStock = await _db.ProductColorStocks
                    .FirstOrDefaultAsync(cs => cs.ProductId == productId && cs.Color == color);
                if (colorStock != null)
                    availableStock = colorStock.Stock;
            }

            var item = await _db.CartItems
                .FirstOrDefaultAsync(c => c.SessionId == sid && c.ProductId == productId && c.Color == color);

            int currentQty = item?.Quantity ?? 0;
            if (currentQty + qty > availableStock)
            {
                TempData["Error"] = availableStock == 0
                    ? $"Sorry, {(string.IsNullOrWhiteSpace(color) ? "this item" : color)} is out of stock."
                    : $"Sorry, only {availableStock} in stock for {(string.IsNullOrWhiteSpace(color) ? "this item" : color)}.";
                var referer = Request.Headers.ContainsKey("Referer") ? Request.Headers["Referer"].ToString() : "/";
                return Redirect(string.IsNullOrEmpty(referer) ? "/" : referer);
            }

            if (item == null)
                _db.CartItems.Add(new CartItem { SessionId = sid, ProductId = productId, Quantity = qty, Color = color });
            else
                item.Quantity += qty;

            await _db.SaveChangesAsync();
            TempData["Success"] = "Item added to cart!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Update(int cartItemId, int qty)
        {
            var item = await _db.CartItems.FindAsync(cartItemId);
            if (item != null)
            {
                if (qty <= 0) _db.CartItems.Remove(item);
                else item.Quantity = qty;
                await _db.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Remove(int cartItemId)
        {
            var item = await _db.CartItems.FindAsync(cartItemId);
            if (item != null) { _db.CartItems.Remove(item); await _db.SaveChangesAsync(); }
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Checkout()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "Please login or register to place an order.";
                return RedirectToAction("Login", "Account", new { returnUrl = "/Cart/Checkout" });
            }

            var sid = GetSession();
            var items = await _db.CartItems
                .Include(c => c.Product).ThenInclude(p => p!.Category)
                .Where(c => c.SessionId == sid).ToListAsync();

            if (!items.Any()) return RedirectToAction("Index");
            return View(items);
        }

        [HttpPost]
        public async Task<IActionResult> ValidateCoupon([FromBody] CouponValidateRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Code))
                return Json(new { valid = false, message = "Please enter a coupon code." });

            // Find coupon by code (case-insensitive)
            var cp = await _db.Coupons.FirstOrDefaultAsync(c =>
                c.Code.ToLower() == req.Code.Trim().ToLower());

            if (cp == null)
                return Json(new { valid = false, message = "Coupon code not found." });

            if (!cp.IsActive)
                return Json(new { valid = false, message = "This coupon is no longer active." });

            if (cp.ExpiryDate != null && cp.ExpiryDate < DateTime.Now)
                return Json(new { valid = false, message = "This coupon has expired." });

            if (req.Subtotal < cp.MinOrderAmt)
                return Json(new { valid = false, message = $"This coupon requires a minimum order of ${cp.MinOrderAmt:0.00}. Your subtotal is ${req.Subtotal:0.00}." });

            if (cp.UsageLimit != null && cp.UsageLimit > 0 && cp.UsedCount >= cp.UsageLimit)
                return Json(new { valid = false, message = "This coupon has reached its usage limit." });

            decimal disc = cp.DiscountType == "Percent"
                ? Math.Round(req.Subtotal * cp.DiscountValue / 100, 2)
                : cp.DiscountValue;

            return Json(new { valid = true, discount = disc, message = $"Coupon applied! You save ${disc:0.00}." });
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder(string name, string phone,
            string address, string payment, string? deliveryMethod, string? note, string? couponCode,
            double? locationLat, double? locationLng, string? city, IFormFile? invoiceFile)
        {
            var isPhnomPenh = (city ?? "").Contains("Phnom Penh", StringComparison.OrdinalIgnoreCase)
                || (address ?? "").Contains("Phnom Penh", StringComparison.OrdinalIgnoreCase);

            if (!isPhnomPenh && string.IsNullOrEmpty(deliveryMethod))
            {
                TempData["Error"] = "សូមជ្រើសរើសមធ្យោបាយដឹកជញ្ជូន (Delivery Method).";
                return RedirectToAction("Checkout");
            }

            // Validate COD is only for Phnom Penh
            if (payment == "Cash")
            {
                if (!isPhnomPenh)
                {
                    TempData["Error"] = "Cash on Delivery is only available in Phnom Penh. Please choose another payment method.";
                    return RedirectToAction("Checkout");
                }
            }

            var sid = GetSession();
            var items = await _db.CartItems.Include(c => c.Product)
                                  .Where(c => c.SessionId == sid).ToListAsync();
            if (!items.Any()) return RedirectToAction("Index");

            decimal sub = items.Sum(i => i.Product!.Price * i.Quantity);
            decimal disc = 0;

            if (!string.IsNullOrWhiteSpace(couponCode))
            {
                var cp = await _db.Coupons.FirstOrDefaultAsync(c =>
                    c.Code.ToLower() == couponCode.Trim().ToLower() && c.IsActive &&
                    (c.ExpiryDate == null || c.ExpiryDate >= DateTime.Now) &&
                    sub >= c.MinOrderAmt &&
                    (c.UsageLimit == null || c.UsageLimit == 0 || c.UsedCount < c.UsageLimit));
                if (cp != null)
                {
                    disc = cp.DiscountType == "Percent"
                        ? Math.Round(sub * cp.DiscountValue / 100, 2)
                        : cp.DiscountValue;
                    cp.UsedCount++;
                }
            }

            decimal ship = (sub - disc) >= 100 ? 0 : 2;

            int? customerId = null;
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out int uid))
                customerId = uid;

            // Save invoice file if provided (ABA Pay)
            string? invoicePath = null;
            if (invoiceFile != null && invoiceFile.Length > 0)
            {
                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "invoices");
                Directory.CreateDirectory(uploadsDir);
                var ext = Path.GetExtension(invoiceFile.FileName);
                var fileName = $"invoice_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid():N}{ext}";
                var filePath = Path.Combine(uploadsDir, fileName);
                using (var stream = System.IO.File.Create(filePath))
                    await invoiceFile.CopyToAsync(stream);
                invoicePath = $"/invoices/{fileName}";
            }

            // Auto-increment order number: ODR-YYMMDD-XXXX (resets each day)
            var today = DateTime.Now;
            var datePrefix = today.ToString("yyMMdd");
            var todayStart = today.Date;
            var todayEnd = todayStart.AddDays(1);
            var todayOrderCount = await _db.Orders
                .Where(o => o.CreatedAt >= todayStart && o.CreatedAt < todayEnd)
                .CountAsync();
            var seqNum = (todayOrderCount + 1).ToString("D4");
            var orderNumber = $"ODR-{datePrefix}-{seqNum}";

            var order = new Order
            {
                OrderNumber = orderNumber,
                CustomerId = customerId,
                SubTotal = sub,
                Discount = disc,
                ShippingFee = ship,
                Total = sub - disc + ship,
                ShipName = name,
                ShipPhone = phone,
                ShipAddress = address,
                PaymentMethod = payment,
                DeliveryMethod = deliveryMethod,
                Note = note,
                CouponCode = couponCode,
                LocationLat = locationLat,
                LocationLng = locationLng,
                InvoicePath = invoicePath,
                Status = "Pending"
            };
            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            foreach (var i in items)
            {
                _db.OrderItems.Add(new OrderItem
                {
                    OrderId = order.OrderId,
                    ProductId = i.ProductId,
                    ProductName = i.Product!.Name,
                    ProductImage = i.Product.ImageUrl,
                    Color = i.Color,
                    Quantity = i.Quantity,
                    UnitPrice = i.Product.Price,
                    TotalPrice = i.Product.Price * i.Quantity
                });

                // Deduct from color stock if applicable, then always deduct total product stock
                if (!string.IsNullOrWhiteSpace(i.Color))
                {
                    var colorStock = await _db.ProductColorStocks
                        .FirstOrDefaultAsync(cs => cs.ProductId == i.ProductId && cs.Color == i.Color);
                    if (colorStock != null)
                        colorStock.Stock = Math.Max(0, colorStock.Stock - i.Quantity);
                }
                i.Product.Stock = Math.Max(0, i.Product.Stock - i.Quantity);
                i.Product.SoldCount += i.Quantity;
            }
            _db.CartItems.RemoveRange(items);
            await _db.SaveChangesAsync();

            // ── Telegram Notifications ──────────────────────────────────────
            var botToken = _config["Telegram:BotToken"] ?? "";
            var groupChat = _config["Telegram:GroupChatId"] ?? "";

            _logger.LogInformation("Telegram config — BotToken empty={BotEmpty}, GroupChatId={GroupChat}",
                string.IsNullOrEmpty(botToken), groupChat);

            if (!string.IsNullOrEmpty(botToken))
            {
                // Build item list for admin message
                var itemLines = string.Join("\n", items.Select(i => $"▪ {i.Product!.Name} x{i.Quantity}"));

                // 1️⃣  Admin group alert — "New Order" in Khmer + English
                var adminMsg =
                    $"🛍️ <b>មានការបញ្ជាទិញថ្មី! / New Order</b>\n\n" +
                    $"📋 <b>លេខបញ្ជា:</b> {order.OrderNumber}\n" +
                    $"👤 <b>អតិថិជន:</b> {order.ShipName}\n" +
                    $"📱 <b>ទូរស័ព្ទ:</b> {order.ShipPhone}\n" +
                    $"📍 <b>អាសយដ្ឋាន:</b> {order.ShipAddress}\n\n" +
                    $"🛒 <b>ទំនិញ:</b>\n{itemLines}\n\n" +

                    $"💳 <b>វិធីបង់:</b> {payment}\n" +
                    $"🧾 <b>តម្លៃសរុប:</b> ${order.SubTotal:F2}\n" +
                    $"🏷️ <b>បញ្ចុះតម្លៃ:</b> -${order.Discount}\n" +
                    $"💰 <b>ត្រូវបង់:</b> ${order.Total:F2}\n"+
                    (order.Note != null ? $"📝 <b>Note:</b> {order.Note}\n" : "") +
                    $"\n🕐 {order.CreatedAt:dd/MM/yyyy HH:mm}";

                if (!string.IsNullOrEmpty(groupChat))
                    await SendTelegramMessage(botToken, groupChat, adminMsg);
                else
                    _logger.LogWarning("Telegram: GroupChatId is empty, group alert skipped");

                // 2️⃣  Customer — "Order success" in Khmer (only if TelegramChatId linked)
                if (customerId.HasValue)
                {
                    var cust = await _db.Customers.FindAsync(customerId.Value);
                    _logger.LogInformation("Customer TelegramChatId={TgId}", cust?.TelegramChatId ?? "null");
                    if (cust != null && !string.IsNullOrEmpty(cust.TelegramChatId))
                    {
                        var custMsg =
                            $"✅ <b>ការបញ្ជាទិញជោគជ័យ!</b>\n\n" +
                            $"📋 <b>លេខបញ្ជា:</b> {order.OrderNumber}\n" +
                            $"👤 <b>ឈ្មោះ:</b> {order.ShipName}\n" +
                            $"📱 <b>ទូរស័ព្ទ:</b> {order.ShipPhone}\n" +
                            $"📍 <b>អាសយដ្ឋាន:</b> {order.ShipAddress}\n\n" +

                            $"🛒 <b>ទំនិញ:</b>\n{string.Join("\n", items.Select(i => $"▪ {i.Product!.Name} x{i.Quantity}"))}\n\n" +

                            $"💵 <b>តម្លៃសរុប:</b> ${order.SubTotal:F2}\n" +
                            $"🎯 <b>បញ្ចុះតម្លៃ:</b> -${order.Discount:F2}\n" +
                            $"🚚 <b>ដឹកជញ្ជូន:</b> ${order.ShippingFee:F2}\n" +
                            $"💰 <b>ត្រូវបង់:</b> ${order.Total:F2}\n\n" +

                            $"💳 <b>វិធីបង់:</b> {payment}\n" +

                            (payment == "ABA Pay"
                                ? "\n🏦 <b>ABA Pay:</b>\nសូមស្កេន QR ហើយផ្ញើរវិកិយបត្រ (Invoice)\nពួកយើងនឹងពិនិត្យឲ្យអ្នកភ្លាមៗ 🙏\n"
                                : "") +

                            $"\n📦 <b>ស្ថានភាព:</b> {order.Status}\n" +
                            $"🕐 <b>កាលបរិច្ឆេទ:</b> {DateTime.Now:dd/MM/yyyy HH:mm}\n\n" +

                            $"\n🙏 អរគុណសម្រាប់ការទិញពី <b>Pink Daisy Shop</b>!\n" +
                            $"📩 ប្រសិនបើមានសំណួរ សូមទាក់ទងយើងវិញបានគ្រប់ពេល។\n" +
                            $"📲 <b>Telegram:</b> <a href='https://t.me/Pha_Rie'>@Pha_Rie</a>";
                        await SendTelegramMessage(botToken, cust.TelegramChatId, custMsg);
                    }
                }
            }
            else
            {
                _logger.LogWarning("Telegram: BotToken is empty, all notifications skipped");
            }
            // ─────────────────────────────────────────────────────────────────

            HttpContext.Session.Remove("CartSession");
            TempData["OrderNumber"] = order.OrderNumber;
            TempData["OrderId"] = order.OrderId;
            TempData["PaymentMethod"] = payment;
            return RedirectToAction("Confirmation");
        }

        public IActionResult Confirmation() => View();

        // ====== ORDER HISTORY ======
        public async Task<IActionResult> MyOrders()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account", new { returnUrl = "/Cart/MyOrders" });

            int cid = int.Parse(userId);
            var orders = await _db.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.CustomerId == cid)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
            return View(orders);
        }

        public async Task<IActionResult> MyOrderDetail(int id)
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            int cid = int.Parse(userId);
            var order = await _db.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == id && o.CustomerId == cid);
            if (order == null) return NotFound();
            return View(order);
        }

        // ====== TELEGRAM BOT WEBHOOK ======
        [HttpPost]
        public async Task<IActionResult> TelegramWebhook([FromBody] TelegramUpdate update)
        {
            try
            {
                var text = update?.Message?.Text ?? "";
                var chatId = update?.Message?.Chat?.Id.ToString() ?? "";
                if (string.IsNullOrEmpty(chatId)) return Ok();

                if (text.StartsWith("/start"))
                {
                    var parts = text.Split(' ');
                    var botToken = _config["Telegram:BotToken"] ?? "";
                    if (parts.Length >= 2 && int.TryParse(parts[1], out int cid))
                    {
                        var customer = await _db.Customers.FindAsync(cid);
                        if (customer != null)
                        {
                            customer.TelegramChatId = chatId;
                            await _db.SaveChangesAsync();
                            await SendTelegramMessage(botToken, chatId,
                                $"✅ Hi <b>{customer.FullName}</b>! Your account is now linked.\n\nYou will receive order status updates here automatically. 🎉");
                        }
                    }
                    else
                    {
                        await SendTelegramMessage(_config["Telegram:BotToken"] ?? "", chatId,
                            "👋 Welcome to <b>Pink Daisy Shop</b>!\n\nTo link your account, go to your order confirmation page and click <b>'Connect Telegram'</b>.");
                    }
                }
            }
            catch (Exception ex) { _logger.LogError(ex, "TelegramWebhook error"); }
            return Ok();
        }

        public async Task SendTelegramMessage(string botToken, string chatId, string message)
        {
            if (string.IsNullOrEmpty(botToken) || string.IsNullOrEmpty(chatId))
            {
                _logger.LogWarning("Telegram: skipped — botToken or chatId is empty. chatId={ChatId}", chatId);
                return;
            }
            try
            {
                var url = $"https://api.telegram.org/bot{botToken}/sendMessage";
                var http = _httpClientFactory.CreateClient("telegram");
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("chat_id", chatId),
                    new KeyValuePair<string, string>("text", message),
                    new KeyValuePair<string, string>("parse_mode", "HTML")
                });
                var response = await http.PostAsync(url, content);
                var body = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                    _logger.LogError("Telegram API error: {Status} {Body}", response.StatusCode, body);
                else
                    _logger.LogInformation("Telegram sent OK to {ChatId}", chatId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Telegram send failed to {ChatId}", chatId);
            }
        }

        // Static overload kept for AdminController compatibility
        public static async Task SendTelegramMessage(string botToken, string chatId, string message, ILogger? logger = null)
        {
            if (string.IsNullOrEmpty(botToken) || string.IsNullOrEmpty(chatId))
            {
                logger?.LogWarning("Telegram: skipped — botToken or chatId is empty. chatId={ChatId}", chatId);
                return;
            }
            try
            {
                var url = $"https://api.telegram.org/bot{botToken}/sendMessage";
                using var http = new HttpClient();
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("chat_id", chatId),
                    new KeyValuePair<string, string>("text", message),
                    new KeyValuePair<string, string>("parse_mode", "HTML")
                });
                var response = await http.PostAsync(url, content);
                var body = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                    logger?.LogError("Telegram API error: {Status} {Body}", response.StatusCode, body);
                else
                    logger?.LogInformation("Telegram sent OK to {ChatId}", chatId);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Telegram send failed to {ChatId}", chatId);
            }
        }

    }

    public class CouponValidateRequest
    {
        public string Code { get; set; } = "";
        public decimal Subtotal { get; set; }
    }
}