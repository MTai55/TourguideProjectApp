using Microsoft.AspNetCore.Mvc;
using TourismApp.Web.Models;
using TourismApp.Web.Filters;
using TourismApp.Web.Services;

namespace TourismApp.Web.Controllers.Admin;


[AdminOnly]
public class AdminDashboardController(ApiService api) : Controller
{
    public async Task<IActionResult> Index()
    {
        var stats = await api.GetAdminStatsAsync();
        return View(stats);
    }

    public async Task<IActionResult> Map()
    {
        var result = await api.GetAdminPlacesAsync(pendingOnly: false);
        ViewBag.IsAdmin = true;
        return View("~/Views/Places/Map.cshtml", result ?? new List<PlaceViewModel>());
    }
}