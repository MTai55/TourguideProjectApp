using Microsoft.AspNetCore.Mvc;
using TourismApp.Web.Services;
using TourismApp.Web.Filters;
using TourismApp.Web.Models;

namespace TourismApp.Web.Controllers;

public class DashboardController(ApiService api) : Controller
{
    public async Task<IActionResult> Index()
    {
        if (HttpContext.Session.GetString("JwtToken") == null)
            return RedirectToAction("Login", "Auth");

        var dashboard = await api.GetDashboardAsync();
        return View(dashboard);
    }
}