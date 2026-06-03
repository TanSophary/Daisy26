// Program.cs
using Microsoft.EntityFrameworkCore;
using OnlineShop.Data;
using OnlineShop.Filters;
using OnlineShop.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddScoped<CartCountFilter>();
builder.Services.AddScoped<OnlineShop.Services.CodeGeneratorService>();
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.AddService<CartCountFilter>();
});
builder.Services.AddHttpClient("telegram");

// SQL Server connection
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Session for cart
builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddHttpContextAccessor();

// Telegram polling background service (no webhook/ngrok needed)
builder.Services.AddHostedService<TelegramPollingService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
