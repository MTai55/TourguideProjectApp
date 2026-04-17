using TourismApp.Web.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.ResponseCompression;

var builder = WebApplication.CreateBuilder(args);

// ── MVC ───────────────────────────────────────────────────────
builder.Services.AddControllersWithViews();

// ── Response Compression (gzip/brotli) ──────────────────────────
builder.Services.AddResponseCompression(options => {
    options.EnableForHttps = true;
    options.Providers.Add<GzipCompressionProvider>();
    options.Providers.Add<BrotliCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/json", "text/javascript", "image/svg+xml", "font/woff2" }
    );
});

// ── Memory Cache (API response caching) ────────────────────────
builder.Services.AddMemoryCache();

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
    client.Timeout = TimeSpan.FromSeconds(10);
});

// ── HttpContextAccessor (đọc session trong service) ───────────
builder.Services.AddHttpContextAccessor();

builder.Services.AddDataProtection()
    .SetApplicationName("TourismApp.Web");

var app = builder.Build();

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Home/Error");

// ── Enable Response Compression ──────────────────────────────
app.UseResponseCompression();

// ── Static Files with Cache Headers ─────────────────────────
app.UseStaticFiles(new StaticFileOptions {
    OnPrepareResponse = ctx => {
        const int cacheMaxAge = 31536000; // 1 year
        ctx.Context.Response.Headers.CacheControl = $"public, max-age={cacheMaxAge}, immutable";
        // Add SRI for security
        if (ctx.File.PhysicalPath?.EndsWith(".js") == true || 
            ctx.File.PhysicalPath?.EndsWith(".css") == true)
        {
            ctx.Context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        }
    }
});

app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// ── Area routing (Admin routes) ───────────────────────────────
app.MapAreaControllerRoute(
    name: "admin",
    areaName: "Admin",
    pattern: "Admin/{controller=Dashboard}/{action=Index}/{id?}");

// ── Default routing ───────────────────────────────────────────
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();