using Microsoft.AspNetCore.Mvc;
using TourismApp.Web.Models;
using TourismApp.Web.Filters;
using TourismApp.Web.Services;

namespace TourismApp.Web.Controllers.Admin;


[AdminOnly]
public class AdminPlacesController(ApiService api) : Controller
{
    // Danh sách quán chờ duyệt
    public async Task<IActionResult> Index(bool pendingOnly = false)
    {
        var result = await api.GetAdminPlacesAsync(pendingOnly);
        ViewBag.PendingOnly = pendingOnly;
        return View(result ?? new List<PlaceViewModel>());  // ← List trực tiếp
    }

    // Duyệt quán
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        await api.ApprovePlaceAsync(id);
        TempData["Success"] = "Đã duyệt quán!";
        return RedirectToAction("Index");
    }

    // Từ chối quán
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id)
    {
        await api.RejectPlaceAsync(id);
        TempData["Success"] = "Đã từ chối quán!";
        return RedirectToAction("Index");
    }

    // Tạm khóa quán
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Suspend(int id)
    {
        await api.SuspendPlaceAsync(id);
        TempData["Success"] = "Đã tạm khóa quán!";
        return RedirectToAction("Index");
    }
}
