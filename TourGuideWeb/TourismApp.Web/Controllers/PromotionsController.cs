using Microsoft.AspNetCore.Mvc;
using TourismApp.Web.Filters;
using TourismApp.Web.Services;
using TourismApp.Web.Models;

namespace TourismApp.Web.Controllers;

[OwnerOnly]
[Obsolete("Promotions feature has been disabled")]
public class PromotionsController : Controller
{
    // Promotions functionality has been removed
    public IActionResult Index()
    {
        return NotFound();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(CreatePromotionViewModel vm)
    {
        return NotFound();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(int id)
    {
        return NotFound();
    }
}