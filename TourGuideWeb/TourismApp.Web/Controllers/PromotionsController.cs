using Microsoft.AspNetCore.Mvc;
using TourismApp.Web.Filters;
using TourismApp.Web.Services;
using TourismApp.Web.Models;

namespace TourismApp.Web.Controllers;

[OwnerOnly]
public class PromotionsController(ApiService api, ILogger<PromotionsController> logger) : Controller
{
    public async Task<IActionResult> Index()
    {
        var places = await api.GetMyPlacesAsync();
        var promos = await api.GetPromotionsAllAsync();
        
        if (places?.Items != null)
        {
            logger.LogInformation($"📍 Loaded {places.Items.Count} places");
            ViewBag.Places = places.Items;
        }
        else
        {
            logger.LogWarning("⚠️ Failed to load places for promotions");
            ViewBag.Places = new List<PlaceViewModel>();
        }
        
        if (promos != null)
        {
            logger.LogInformation($"🎁 Loaded {promos.Count} promotions");
        }
        else
        {
            logger.LogWarning("⚠️ Failed to load promotions");
        }
        
        return View(promos ?? new List<PromotionViewModel>());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreatePromotionViewModel vm)
    {
        logger.LogInformation($"📤 Creating promotion: {vm.Title} for place {vm.PlaceId}");
        
        if (!ModelState.IsValid)
        {
            var errors = string.Join("; ", ModelState.Values
                .SelectMany(x => x.Errors)
                .Select(x => x.ErrorMessage));
            logger.LogWarning($"❌ ModelState invalid: {errors}");
            
            // Reload places for dropdown
            var places = await api.GetMyPlacesAsync();
            ViewBag.Places = places?.Items ?? new List<PlaceViewModel>();
            return View(vm);
        }
        
        var (success, promo, error) = await api.CreatePromotionAsync(vm);
        if (!success)
        {
            logger.LogError($"❌ API Error creating promotion: {error}");
            TempData["Error"] = $"Lỗi: {error}";
            
            // Reload places for retry
            var places = await api.GetMyPlacesAsync();
            ViewBag.Places = places?.Items ?? new List<PlaceViewModel>();
            return View(vm);
        }
        
        logger.LogInformation($"✅ Promotion created: {promo?.PromoId}");
        TempData["Success"] = "Đã tạo khuyến mãi!";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        logger.LogInformation($"🗑️ Deleting promotion {id}");
        
        var success = await api.DeletePromotionAsync(id);
        if (!success)
        {
            logger.LogError($"❌ Failed to delete promotion {id}");
            TempData["Error"] = "Xóa khuyến mãi thất bại!";
            return RedirectToAction("Index");
        }
        
        logger.LogInformation($"✅ Promotion {id} deleted");
        TempData["Success"] = "Đã xóa khuyến mãi!";
        return RedirectToAction("Index");
    }
}