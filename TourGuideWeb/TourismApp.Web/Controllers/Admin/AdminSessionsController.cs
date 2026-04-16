using Microsoft.AspNetCore.Mvc;
using TourismApp.Web.Filters;
using TourismApp.Web.Services;

namespace TourismApp.Web.Controllers.Admin;

[Area("Admin")]
[AdminOnly]
public class AdminSessionsController(ApiService api) : Controller
{
    public async Task<IActionResult> Index(string status = "pending", int page = 1, string? search = null)
    {
        var stats   = await api.GetSessionStatsAsync();
        var result  = await api.GetSessionsAsync(status, page, search);

        ViewBag.Status     = status;
        ViewBag.Search     = search ?? string.Empty;
        ViewBag.Page       = page;
        ViewBag.TotalPages = (int)Math.Ceiling((result?.Total ?? 0) / 20.0);
        ViewBag.Stats      = stats;

        return View(result?.Items ?? new List<ApiService.SessionDto>());
    }

    [HttpPost]
    public async Task<IActionResult> Activate(Guid sessionId, string returnStatus = "pending")
    {
        var (ok, err) = await api.ActivateSessionAsync(sessionId);
        TempData[ok ? "Success" : "Error"] = ok
            ? "Đã kích hoạt thành công!"
            : $"Lỗi: {err}";
        return RedirectToAction(nameof(Index), new { status = returnStatus });
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(Guid sessionId)
    {
        var (ok, err) = await api.DeleteSessionAsync(sessionId);
        TempData[ok ? "Success" : "Error"] = ok
            ? "Đã hủy session."
            : $"Lỗi: {err}";
        return RedirectToAction(nameof(Index), new { status = "pending" });
    }

    [HttpPost]
    public async Task<IActionResult> Deactivate(Guid sessionId, string returnStatus = "active")
    {
        var (ok, err) = await api.DeactivateSessionAsync(sessionId);
        TempData[ok ? "Success" : "Error"] = ok
            ? "Đã thu hồi quyền truy cập."
            : $"Lỗi: {err}";
        return RedirectToAction(nameof(Index), new { status = returnStatus });
    }
}
