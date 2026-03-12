using Microsoft.AspNetCore.Mvc;
using TourismApp.Web.Services;

namespace TourismApp.Web.Controllers;

public class DashboardController(ApiService api) : Controller
{
    // GET /Dashboard
    public async Task<IActionResult> Index()
    {
        // Kiểm tra đã đăng nhập chưa
        if (HttpContext.Session.GetString("JwtToken") == null)
            return RedirectToAction("Login", "Auth");

        var dashboard = await api.GetDashboardAsync();
        return View(dashboard);
    }
}