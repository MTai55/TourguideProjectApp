using Microsoft.AspNetCore.Mvc;
using TourismApp.Web.Filters;
using TourismApp.Web.Services;
using TourismApp.Web.Models;

namespace TourismApp.Web.Controllers.Admin;

[Area("Admin")]
[AdminOnly]
[Obsolete("Review moderation feature has been disabled")]
public class AdminReviewsController : Controller
{
    // Reviews functionality has been removed
    public IActionResult Index(bool hiddenOnly = false)
    {
        return NotFound();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Hide(int reviewId, string note)
    {
        return NotFound();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Show(int reviewId)
    {
        return NotFound();
    }
}