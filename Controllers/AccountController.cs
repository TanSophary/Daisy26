using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Data;
using OnlineShop.Models;
using OnlineShop.Services;
using System.Security.Cryptography;
using System.Text;

namespace OnlineShop.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly CodeGeneratorService _codes;
        public AccountController(ApplicationDbContext db, CodeGeneratorService codes) { _db = db; _codes = codes; }

        private static string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password + "PinkDaisy_Salt"));
            return Convert.ToBase64String(bytes);
        }

        // ====== REGISTER ======
        public IActionResult Register(string? returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string fullName, string email, string phone,
            string password, string confirmPassword, string? returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            { TempData["Error"] = "Please fill in all required fields."; return View(); }

            if (password != confirmPassword)
            { TempData["Error"] = "Passwords do not match."; return View(); }

            if (await _db.Customers.AnyAsync(c => c.Email == email))
            { TempData["Error"] = "This email is already registered."; return View(); }

            var customer = new Customer
            {
                FullName = fullName,
                Email = email,
                Phone = phone,
                PasswordHash = HashPassword(password),
                CardCode = await _codes.NextCardCodeAsync(),
                IsActive = true,
                CreatedAt = DateTime.Now
            };
            _db.Customers.Add(customer);
            await _db.SaveChangesAsync();

            HttpContext.Session.SetString("UserId", customer.CustomerId.ToString());
            HttpContext.Session.SetString("UserName", customer.FullName);
            HttpContext.Session.SetString("UserEmail", customer.Email);

            TempData["Success"] = $"Welcome, {customer.FullName}! Your account has been created.";
            return Redirect(returnUrl ?? "/");
        }

        // ====== LOGIN ======
        public IActionResult Login(string? returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password, string? returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            var hash = HashPassword(password);
            var customer = await _db.Customers
                .FirstOrDefaultAsync(c => c.Email == email && c.PasswordHash == hash && c.IsActive);

            if (customer == null)
            { TempData["Error"] = "Invalid email or password."; return View(); }

            HttpContext.Session.SetString("UserId", customer.CustomerId.ToString());
            HttpContext.Session.SetString("UserName", customer.FullName);
            HttpContext.Session.SetString("UserEmail", customer.Email);

            TempData["Success"] = $"Welcome back, {customer.FullName}!";
            return Redirect(returnUrl ?? "/");
        }

        // ====== LOGOUT ======
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("UserId");
            HttpContext.Session.Remove("UserName");
            HttpContext.Session.Remove("UserEmail");
            TempData["Success"] = "You have been logged out.";
            return RedirectToAction("Index", "Home");
        }

        // ====== FORGOT PASSWORD (OTP) ======
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Email == email && c.IsActive);
            if (customer == null)
            {
                TempData["Error"] = "No account found with that email address.";
                return View();
            }

            // Generate 6-digit OTP
            var otp = new Random().Next(100000, 999999).ToString();
            customer.OtpCode = otp;
            customer.OtpExpiry = DateTime.Now.AddMinutes(10);
            await _db.SaveChangesAsync();

            // Store email in session for OTP verification step
            HttpContext.Session.SetString("OtpEmail", email);

            // In production: send OTP via email/SMS. For dev: show it in TempData.
            TempData["OtpCode"] = otp; // Remove this in production!
            TempData["Success"] = $"OTP sent! (Dev mode: your OTP is shown below)";
            return RedirectToAction("VerifyOtp");
        }

        // ====== VERIFY OTP ======
        public IActionResult VerifyOtp()
        {
            var email = HttpContext.Session.GetString("OtpEmail");
            if (string.IsNullOrEmpty(email)) return RedirectToAction("ForgotPassword");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> VerifyOtp(string otp)
        {
            var email = HttpContext.Session.GetString("OtpEmail");
            if (string.IsNullOrEmpty(email)) return RedirectToAction("ForgotPassword");

            var customer = await _db.Customers.FirstOrDefaultAsync(c =>
                c.Email == email && c.OtpCode == otp && c.OtpExpiry > DateTime.Now);
            if (customer == null)
            {
                TempData["Error"] = "Invalid or expired OTP. Please try again.";
                return View();
            }

            // OTP valid – store a short-lived token for the reset step
            var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("/", "_").Replace("+", "-").Replace("=", "");
            customer.PasswordResetToken = token;
            customer.PasswordResetExpiry = DateTime.Now.AddMinutes(15);
            customer.OtpCode = null;
            customer.OtpExpiry = null;
            await _db.SaveChangesAsync();

            HttpContext.Session.Remove("OtpEmail");
            return RedirectToAction("ResetPassword", new { token });
        }

        public async Task<IActionResult> ResetPassword(string token)
        {
            var customer = await _db.Customers.FirstOrDefaultAsync(c =>
                c.PasswordResetToken == token && c.PasswordResetExpiry > DateTime.Now);
            if (customer == null)
            {
                TempData["Error"] = "Reset link is invalid or expired.";
                return RedirectToAction("Login");
            }
            ViewBag.Token = token;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(string token, string password, string confirmPassword)
        {
            if (password != confirmPassword)
            {
                TempData["Error"] = "Passwords do not match.";
                ViewBag.Token = token;
                return View();
            }
            var customer = await _db.Customers.FirstOrDefaultAsync(c =>
                c.PasswordResetToken == token && c.PasswordResetExpiry > DateTime.Now);
            if (customer == null)
            {
                TempData["Error"] = "Reset link is invalid or expired.";
                return RedirectToAction("Login");
            }
            customer.PasswordHash = HashPassword(password);
            customer.PasswordResetToken = null;
            customer.PasswordResetExpiry = null;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Password reset successfully! Please login.";
            return RedirectToAction("Login");
        }

        // ====== PROFILE ======
        public async Task<IActionResult> Profile()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", new { returnUrl = "/Account/Profile" });
            var customer = await _db.Customers
                .Include(c => c.Orders)
                .FirstOrDefaultAsync(c => c.CustomerId == int.Parse(userId));
            if (customer == null) return RedirectToAction("Login");
            return View(customer);
        }
    }
}