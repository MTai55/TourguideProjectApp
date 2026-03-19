using TourismApp.Web.Services;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// ── MVC ───────────────────────────────────────────────────────
builder.Services.AddControllersWithViews();

// ── Session (lưu JWT token) ───────────────────────────────────
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ── Cookie Authentication ─────────────────────────────────────
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options => {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });
builder.Services.AddAuthorization();

// ── HttpClient gọi TourGuideAPI ───────────────────────────────
var apiBase = builder.Configuration["ApiSettings:BaseUrl"]!;
Console.WriteLine($">>> API Base URL: {apiBase}");
builder.Services.AddHttpClient<ApiService>(client => {
    client.BaseAddress = new Uri(apiBase);
});

// ── HttpContextAccessor (đọc session trong service) ───────────
builder.Services.AddHttpContextAccessor();

builder.Services.AddDataProtection()
    .SetApplicationName("TourismApp.Web");

var app = builder.Build();

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Home/Error");

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();