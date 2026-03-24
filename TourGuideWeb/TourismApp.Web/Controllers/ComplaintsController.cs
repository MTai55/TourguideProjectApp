using Microsoft.AspNetCore.Mvc;
using TourismApp.Web.Filters;
using TourismApp.Web.Models;
using TourismApp.Web.Services;

namespace TourismApp.Web.Controllers;

[OwnerOnly]
public class ComplaintsController(ApiService api) : Controller
{
    public async Task<IActionResult> Index()
    {
        var list = await api.GetComplaintsAsync("All");
        return View(list ?? new List<ComplaintViewModel>());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateComplaintViewModel vm)
    {
        await api.CreateComplaintAsync(vm);
        TempData["Success"] = "Đã gửi khiếu nại!";
        return RedirectToAction("Index");
    }
}