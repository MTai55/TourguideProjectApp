using Microsoft.AspNetCore.Mvc;
using TourismApp.Web.Services;
using TourismApp.Web.Filters;
using TourismApp.Web.Models;

namespace TourismApp.Web.Controllers;

[OwnerOnly]
[Obsolete("Review viewing feature has been disabled")]
public class ReviewsController : Controller
{
    // Reviews functionality has been removed
    public IActionResult Index(int? placeId = null)
    {
        return NotFound();
    }

    public IActionResult ByPlace(int placeId)
    {
        return NotFound();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Reply(int reviewId, int placeId, string reply)
    {
        return NotFound();
    }
}