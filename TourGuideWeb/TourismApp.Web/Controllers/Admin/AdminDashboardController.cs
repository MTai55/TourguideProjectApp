using Microsoft.AspNetCore.Mvc;
using TourismApp.Web.Models;
using TourismApp.Web.Filters;
using TourismApp.Web.Services;

namespace TourismApp.Web.Controllers.Admin;

[Area("Admin")]
[AdminOnly]
public class AdminDashboardController(ApiService api, ILogger<AdminDashboardController> logger) : Controller
{
    public async Task<IActionResult> Index()
    {
        try
        {
            logger.LogInformation("🔍[AdminDashboard] Fetching admin stats...");
            var stats = await api.GetAdminStatsAsync();
            logger.LogInformation($"✅ [AdminDashboard] Stats loaded: Users={stats?.TotalUsers}, Places={stats?.TotalPlaces}");
            return View(stats);
        }
        catch (Exception ex)
        {
            logger.LogError($"❌ [AdminDashboard] Error: {ex.Message}\n{ex.StackTrace}");
            return View(null);  // Return empty model to show error on page
        }
    }

    public async Task<IActionResult> Map()
    {
        var result = await api.GetAdminPlacesAsync(pendingOnly: false);
        ViewBag.IsAdmin = true;
        return View("~/Views/Places/Map.cshtml", result ?? new List<PlaceViewModel>());
    }
}