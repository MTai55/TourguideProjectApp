using Microsoft.AspNetCore.Mvc;
using TourismApp.Web.Models;
using TourismApp.Web.Filters;
using TourismApp.Web.Services;

namespace TourismApp.Web.Controllers.Admin;

[Area("Admin")]
[AdminOnly]
public class AdminPlacesController(ApiService api, ILogger<AdminPlacesController> logger) : Controller
{
    // Danh sách quán chờ duyệt
    public async Task<IActionResult> Index(bool pendingOnly = false)
    {
        var result = await api.GetAdminPlacesAsync(pendingOnly);
        ViewBag.PendingOnly = pendingOnly;
        return View(result ?? new List<PlaceViewModel>());  // ← List trực tiếp
    }

    // Chi tiết quán
    public async Task<IActionResult> Detail(int id)
    {
        try
        {
            logger.LogInformation($"📍 Fetching place detail: PlaceId={id}");
            var place = await api.GetPlaceAsync(id);
            if (place == null)
            {
                TempData["Error"] = "Không tìm thấy quán này.";
                return RedirectToAction("Index");
            }
            return View(place);
        }
        catch (Exception ex)
        {
            logger.LogError($"❌ Error fetching place detail: {ex.Message}");
            TempData["Error"] = "Lỗi khi tải thông tin quán.";
            return RedirectToAction("Index");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Suspend(int id)
    {
        await api.SuspendPlaceAsync(id);
        TempData["Success"] = "Đã tạm khóa quán!";
        return RedirectToAction("Index");
    }
}
