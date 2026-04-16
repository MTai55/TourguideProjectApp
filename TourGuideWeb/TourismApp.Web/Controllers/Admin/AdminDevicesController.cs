using Microsoft.AspNetCore.Mvc;
using TourismApp.Web.Filters;
using TourismApp.Web.Services;

namespace TourismApp.Web.Controllers.Admin;

[Area("Admin")]
[AdminOnly]
public class AdminDevicesController(ApiService api) : Controller
{
    // GET /Admin/AdminDevices
    public async Task<IActionResult> Index([FromQuery] int page = 1, [FromQuery] string? search = null)
    {
        var result = await api.GetDeviceStatsAsync(page: page, pageSize: 20, search: search);
        ViewBag.Search    = search ?? string.Empty;
        ViewBag.Page      = page;
        ViewBag.PageSize  = 20;
        ViewBag.Total     = result?.Total ?? 0;
        ViewBag.TotalPages = (int)Math.Ceiling((result?.Total ?? 0) / 20.0);
        return View(result?.Items ?? new List<ApiService.DeviceStatItem>());
    }

    // GET /Admin/AdminDevices/Detail/{deviceId}
    public async Task<IActionResult> Detail(string deviceId)
    {
        var visits = await api.GetDeviceVisitsAsync(deviceId);
        ViewBag.DeviceId = deviceId;
        return View(visits ?? new List<ApiService.DeviceVisitDto>());
    }
}
