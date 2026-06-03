using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using OnlineShop.Data;

namespace OnlineShop.Filters
{
    public class CartCountFilter : IAsyncActionFilter
    {
        private readonly ApplicationDbContext _db;

        public CartCountFilter(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Only run for controllers that use the front-end layout
            var controller = context.Controller as Controller;
            if (controller != null)
            {
                var httpContext = context.HttpContext;
                var sid = httpContext.Session.GetString("CartSession");
                if (!string.IsNullOrEmpty(sid))
                {
                    var count = await _db.CartItems
                        .Where(c => c.SessionId == sid)
                        .SumAsync(c => (int?)c.Quantity) ?? 0;
                    controller.ViewBag.CartCount = count;
                }
                else
                {
                    controller.ViewBag.CartCount = 0;
                }
            }

            await next();
        }
    }
}
