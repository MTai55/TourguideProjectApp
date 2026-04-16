using Microsoft.AspNetCore.Mvc;
using TourismApp.Web.Filters;
using TourismApp.Web.Services;

namespace TourismApp.Web.Controllers.Admin;

[Area("Admin")]
[AdminOnly]
public class AdminPackagesController(ApiService api) : Controller
{
    public async Task<IActionResult> Index()
    {
        var packages = await api.GetAccessPackagesAsync();
        return View(packages ?? new List<ApiService.AccessPackageDto>());
    }

    [HttpPost]
    public async Task<IActionResult> Update(string packageId, double durationHours, int priceVnd, bool isActive, int sortOrder)
    {
        var (ok, err) = await api.UpdateAccessPackageAsync(packageId, durationHours, priceVnd, isActive, sortOrder);
        if (ok)
            TempData["Success"] = $"Đã cập nhật gói {packageId.ToUpper()}";
        else
            TempData["Error"] = $"Lỗi: {err}";
        return RedirectToAction(nameof(Index));
    }
}
