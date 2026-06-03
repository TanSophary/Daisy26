using Microsoft.AspNetCore.Mvc;

namespace OnlineShop.Controllers
{
    public class AdminAuthController : Controller
    {
        // Hard-coded admin credentials (in production use DB + hashed passwords)
        private const string AdminUser = "admin";
        private const string AdminPass = "nTa#102186";

        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("AdminLoggedIn") == "true")
                return RedirectToAction("Dashboard", "Admin");
            return View();
        }

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            if (username == AdminUser && password == AdminPass)
            {
                HttpContext.Session.SetString("AdminLoggedIn", "true");
                HttpContext.Session.SetString("AdminName", "Admin");
                return RedirectToAction("Dashboard", "Admin");
            }
            TempData["Error"] = "Invalid username or password.";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Remove("AdminLoggedIn");
            HttpContext.Session.Remove("AdminName");
            return RedirectToAction("Login");
        }
    }
}