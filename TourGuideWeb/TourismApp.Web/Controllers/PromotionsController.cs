using Microsoft.AspNetCore.Mvc;
using TourismApp.Web.Filters;
using TourismApp.Web.Services;
using TourismApp.Web.Models;

namespace TourismApp.Web.Controllers;

[OwnerOnly]
public class PromotionsController(ApiService api) : Controller
{
    public async Task<IActionResult> Index()
    {
        var places = await api.GetMyPlacesAsync();
        var promos = await api.GetPromotionsAllAsync();
        ViewBag.Places = places?.Items ?? new List<PlaceViewModel>();
        return View(promos ?? new List<PromotionViewModel>());  // ← không null
    }

    public IActionResult Create(int placeId)
    {
        ViewBag.PlaceId = placeId;
        return View(new CreatePromotionViewModel { PlaceId = placeId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreatePromotionViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        await api.CreatePromotionAsync(vm);
        TempData["Success"] = "Đã tạo khuyến mãi!";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await api.DeletePromotionAsync(id);
        TempData["Success"] = "Đã xóa khuyến mãi!";
        return RedirectToAction("Index");
    }
}