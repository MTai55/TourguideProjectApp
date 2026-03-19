using Microsoft.AspNetCore.Mvc;
using TourismApp.Web.Filters;
using TourismApp.Web.Services;

namespace TourismApp.Web.Controllers.Admin;


[AdminOnly]
public class AdminComplaintsController(ApiService api) : Controller
{
    public async Task<IActionResult> Index(string status = "Pending")
    {
        var list = await api.GetComplaintsAsync(status);
        ViewBag.Status = status;
        return View(list ?? []);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Resolve(int id, string note, string action)
    {
        await api.ResolveComplaintAsync(id, note, action);
        TempData["Success"] = "Đã xử lý khiếu nại!";
        return RedirectToAction("Index");
    }
}