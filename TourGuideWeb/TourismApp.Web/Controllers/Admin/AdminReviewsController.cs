using Microsoft.AspNetCore.Mvc;
using TourismApp.Web.Filters;
using TourismApp.Web.Services;
using TourismApp.Web.Models;

namespace TourismApp.Web.Controllers.Admin;

[Area("Admin")]
[AdminOnly]
public class AdminReviewsController(ApiService api) : Controller
{
    public async Task<IActionResult> Index(bool hiddenOnly = false)
    {
        var reviews = await api.GetAllReviewsAsync(hiddenOnly);
        ViewBag.HiddenOnly = hiddenOnly;
        return View(reviews ?? []);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Hide(int reviewId, string note)
    {
        await api.HideReviewAsync(reviewId, note);
        TempData["Success"] = "Đã ẩn review!";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Show(int reviewId)
    {
        await api.ShowReviewAsync(reviewId);
        TempData["Success"] = "Đã hiện lại review!";
        return RedirectToAction("Index");
    }
}