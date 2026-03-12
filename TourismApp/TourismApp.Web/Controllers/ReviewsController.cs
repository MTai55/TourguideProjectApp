using Microsoft.AspNetCore.Mvc;
using TourismApp.Web.Services;

namespace TourismApp.Web.Controllers;

public class ReviewsController(ApiService api) : Controller
{
    // GET /Reviews?placeId=1
    public async Task<IActionResult> Index(int placeId)
    {
        if (HttpContext.Session.GetString("JwtToken") == null)
            return RedirectToAction("Login", "Auth");
        var reviews = await api.GetReviewsAsync(placeId);
        ViewBag.PlaceId = placeId;
        return View(reviews ?? []);
    }

    // POST /Reviews/Reply
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(int reviewId, int placeId, string reply)
    {
        await api.ReplyReviewAsync(reviewId, reply);
        TempData["Success"] = "Đã phản hồi đánh giá!";
        return RedirectToAction("Index", new { placeId });
    }
}