using Microsoft.AspNetCore.Mvc;
using TourismApp.Web.Services;
using TourismApp.Web.Filters;
using TourismApp.Web.Models;

namespace TourismApp.Web.Controllers;

[OwnerOnly]
public class ReviewsController(ApiService api, ILogger<ReviewsController> logger) : Controller
{
    // GET /Reviews — Tất cả review của các quán owner đang quản lý (hoặc 1 quán nếu có placeId)
    public async Task<IActionResult> Index(int? placeId = null)
    {
        if (HttpContext.Session.GetString("JwtToken") == null)
            return RedirectToAction("Login", "Auth");
        
        try
        {
            logger.LogInformation($"📋 Fetching reviews - placeId: {placeId}");
            
            // Nếu có placeId, lấy reviews của quán đó; nếu không, lấy tất cả
            List<ReviewViewModel>? reviews;
            if (placeId.HasValue)
            {
                reviews = await api.GetReviewsAsync(placeId.Value);
            }
            else
            {
                // Lấy tất cả reviews của owner, sau đó có thể filter bằng place
                var places = await api.GetMyPlacesAsync(page: 1);
                reviews = new List<ReviewViewModel>();
                
                if (places?.Items != null)
                {
                    foreach (var place in places.Items)
                    {
                        var placeReviews = await api.GetReviewsAsync(place.PlaceId);
                        if (placeReviews != null)
                        {
                            // Set PlaceName cho mỗi review vì API không trả về
                            foreach (var review in placeReviews)
                            {
                                review.PlaceName = place.Name;
                            }
                            reviews.AddRange(placeReviews);
                        }
                    }
                }
                
                // Sắp xếp theo ngày giảm dần
                reviews = reviews.OrderByDescending(r => r.CreatedAt).ToList();
            }
            
            ViewBag.PlaceId = placeId;
            ViewBag.ShowAllPlaces = !placeId.HasValue;
            return View(reviews ?? new List<ReviewViewModel>());
        }
        catch (Exception ex)
        {
            logger.LogError($"❌ Error fetching reviews: {ex.Message}");
            return View(new List<ReviewViewModel>());
        }
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