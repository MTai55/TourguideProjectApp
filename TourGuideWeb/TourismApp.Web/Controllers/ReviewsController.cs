using Microsoft.AspNetCore.Mvc;
using TourismApp.Web.Services;
using TourismApp.Web.Filters;
using TourismApp.Web.Models;

namespace TourismApp.Web.Controllers;

[OwnerOnly]
public class ReviewsController(ApiService api) : Controller
{
    // GET /Reviews — Tất cả review của các quán owner đang quản lý
    public async Task<IActionResult> Index(int placeId)
    {
        if (HttpContext.Session.GetString("JwtToken") == null)
            return RedirectToAction("Login", "Auth");
        var reviews = await api.GetReviewsAsync(placeId);
        ViewBag.PlaceId = placeId;
        return View(reviews ?? new List<ReviewViewModel>());  // ← đảm bảo không null
    }

    // GET /Reviews/ByPlace/{placeId} — Review của 1 quán cụ thể
    public async Task<IActionResult> ByPlace(int placeId)
    {
        var reviews = await api.GetReviewsAsync(placeId);
        ViewBag.PlaceId = placeId;
        return View("Index", reviews ?? []);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(int reviewId, int placeId, string reply)
    {
        await api.ReplyReviewAsync(reviewId, reply);
        TempData["Success"] = "Đã phản hồi đánh giá!";
        return RedirectToAction("ByPlace", new { placeId });
    }

    // POST Khiếu nại review giả
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ComplainReview(int reviewId, int placeId, string reason)
    {
        await api.CreateComplaintAsync(new CreateComplaintViewModel
        {
            PlaceId = placeId,
            ReviewId = reviewId,
            Type = "fake_review",
            Title = "Review không trung thực",
            Content = reason
        });
        TempData["Success"] = "Đã gửi khiếu nại tới Admin!";
        return RedirectToAction("ByPlace", new { placeId });
    }
}