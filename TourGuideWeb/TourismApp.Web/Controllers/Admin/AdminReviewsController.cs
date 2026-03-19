using Microsoft.AspNetCore.Mvc;
using TourismApp.Web.Filters;
using TourismApp.Web.Services;
using TourismApp.Web.Models;

namespace TourismApp.Web.Controllers.Admin;


[AdminOnly]
public class AdminReviewsController(ApiService api) : Controller
{
    // GET /Admin/AdminReviews
    public async Task<IActionResult> Index([FromQuery] bool hiddenOnly = false)
    {
        var reviews = await api.GetAllReviewsAsync(hiddenOnly);
        ViewBag.HiddenOnly = hiddenOnly;
        return View(reviews ?? []);
    }

    // POST /Admin/AdminReviews/Hide
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Hide(int reviewId, string note)
    {
        await api.HideReviewAsync(reviewId, note);
        TempData["Success"] = "Đã ẩn review!";
        return RedirectToAction("Index");
    }

    // POST /Admin/AdminReviews/Show
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Show(int reviewId)
    {
        await api.ShowReviewAsync(reviewId);
        TempData["Success"] = "Đã hiện review!";
        return RedirectToAction("Index");
    }
}