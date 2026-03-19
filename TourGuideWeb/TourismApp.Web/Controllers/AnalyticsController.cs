using Microsoft.AspNetCore.Mvc;
using TourismApp.Web.Filters;
using TourismApp.Web.Services;
using TourismApp.Web.Models;

[OwnerOnly]
public class AnalyticsController(ApiService api) : Controller
{
    public async Task<IActionResult> Index(int placeId)
    {
        var stats = await api.GetVisitsAnalyticsAsync(placeId);
        ViewBag.PlaceId = placeId;
        return View(stats);
    }
}