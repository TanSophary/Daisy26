using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Data;
using OnlineShop.Models;

namespace OnlineShop.Controllers
{
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _db;
        public ChatController(ApplicationDbContext db) => _db = db;

        // User: open chat widget - returns JSON messages
        [HttpGet]
        public async Task<IActionResult> Messages()
        {
            var sid = HttpContext.Session.GetString("CartSession") ?? "";
            var msgs = await _db.ChatMessages
                .Where(m => m.SessionId == sid)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new { m.Message, m.IsFromAdmin, m.CreatedAt, m.CustomerName })
                .ToListAsync();
            return Json(msgs);
        }

        // User: send message
        [HttpPost]
        public async Task<IActionResult> Send(string message, string? name, string? email)
        {
            var sid = HttpContext.Session.GetString("CartSession");
            if (string.IsNullOrEmpty(sid))
            {
                sid = Guid.NewGuid().ToString();
                HttpContext.Session.SetString("CartSession", sid);
            }
            var userName = HttpContext.Session.GetString("UserName") ?? name ?? "Guest";
            var userEmail = HttpContext.Session.GetString("UserEmail") ?? email ?? "";

            var msg = new ChatMessage
            {
                SessionId = sid,
                CustomerName = userName,
                CustomerEmail = userEmail,
                Message = message,
                IsFromAdmin = false,
                CreatedAt = DateTime.Now
            };
            _db.ChatMessages.Add(msg);
            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        // Admin: list all conversations
        public async Task<IActionResult> AdminInbox()
        {
            if (HttpContext.Session.GetString("AdminLoggedIn") != "true")
                return RedirectToAction("Login", "AdminAuth");

            ViewData["Page"] = "Chat";
            ViewData["Title"] = "Customer Chat";

            var conversations = await _db.ChatMessages
                .GroupBy(m => m.SessionId)
                .Select(g => new
                {
                    SessionId = g.Key,
                    CustomerName = g.FirstOrDefault(m => !m.IsFromAdmin)!.CustomerName,
                    LastMessage = g.OrderByDescending(m => m.CreatedAt).First().Message,
                    LastTime = g.Max(m => m.CreatedAt),
                    Unread = g.Count(m => !m.IsFromAdmin && !m.IsRead)
                })
                .OrderByDescending(g => g.LastTime)
                .ToListAsync();

            ViewBag.Conversations = conversations;
            return View();
        }

        // Admin: reply to a conversation
        [HttpGet]
        public async Task<IActionResult> AdminConversation(string sessionId)
        {
            if (HttpContext.Session.GetString("AdminLoggedIn") != "true")
                return RedirectToAction("Login", "AdminAuth");

            ViewData["Page"] = "Chat";
            ViewData["Title"] = "Chat with Customer";

            var msgs = await _db.ChatMessages
                .Where(m => m.SessionId == sessionId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            // Mark as read
            foreach (var m in msgs.Where(m => !m.IsFromAdmin)) m.IsRead = true;
            await _db.SaveChangesAsync();

            ViewBag.SessionId = sessionId;
            ViewBag.Messages = msgs;
            ViewBag.CustomerName = msgs.FirstOrDefault(m => !m.IsFromAdmin)?.CustomerName ?? "Guest";
            return View();
        }

        // Admin: send reply
        [HttpPost]
        public async Task<IActionResult> AdminReply(string sessionId, string message)
        {
            if (HttpContext.Session.GetString("AdminLoggedIn") != "true")
                return Json(new { error = "Unauthorized" });

            _db.ChatMessages.Add(new ChatMessage
            {
                SessionId = sessionId,
                CustomerName = "Pink Daisy Support",
                Message = message,
                IsFromAdmin = true,
                CreatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();
            return Json(new { success = true });
        }

        // Admin: poll for new messages in a conversation
        [HttpGet]
        public async Task<IActionResult> AdminMessages(string sessionId, int after)
        {
            if (HttpContext.Session.GetString("AdminLoggedIn") != "true")
                return Json(new { error = "Unauthorized" });

            var msgs = await _db.ChatMessages
                .Where(m => m.SessionId == sessionId && m.ChatMessageId > after)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new { m.ChatMessageId, m.Message, m.IsFromAdmin, m.CreatedAt, m.CustomerName })
                .ToListAsync();
            return Json(msgs);
        }

        // User polling
        [HttpGet]
        public async Task<IActionResult> Poll(int after)
        {
            var sid = HttpContext.Session.GetString("CartSession") ?? "";
            var msgs = await _db.ChatMessages
                .Where(m => m.SessionId == sid && m.ChatMessageId > after)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new { m.ChatMessageId, m.Message, m.IsFromAdmin, m.CreatedAt })
                .ToListAsync();
            return Json(msgs);
        }
    }
}