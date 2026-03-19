using Microsoft.AspNetCore.Mvc;
using TourismApp.Web.Models;
using TourismApp.Web.Services;

namespace TourismApp.Web.Controllers;

public class PlacesController(ApiService api) : Controller
{
    private bool IsLoggedIn => HttpContext.Session.GetString("JwtToken") != null;

    public async Task<IActionResult> Index(
        string? search, int page = 1,
        string? categoryId = null,
        string? isApproved = null,
        string? sortBy = null,
        string? district = null,
        string? maxPrice = null)
    {
        if (!IsLoggedIn) return RedirectToAction("Login", "Auth");
        var result = await api.GetPlacesAsync(page, search, categoryId, isApproved, sortBy, district, maxPrice);
        ViewBag.Search = search;
        ViewBag.Page = page;
        ViewBag.Total = result?.Total ?? 0;
        ViewBag.CategoryId = categoryId;
        ViewBag.IsApproved = isApproved;
        ViewBag.SortBy = sortBy;
        ViewBag.District = district;
        ViewBag.MaxPrice = maxPrice;
        return View(result?.Items ?? []);
    }

    public IActionResult Create()
    {
        if (!IsLoggedIn) return RedirectToAction("Login", "Auth");
        return View(new CreatePlaceViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreatePlaceViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var (success, _, error) = await api.CreatePlaceAsync(vm);
        if (!success)
        {
            ModelState.AddModelError("", "Không thể tạo quán. Vui lòng thử lại.");
            return View(vm);
        }
        TempData["Success"] = "Tạo quán thành công! Chờ Admin duyệt.";
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Edit(int id)
    {
        if (!IsLoggedIn) return RedirectToAction("Login", "Auth");
        var place = await api.GetPlaceAsync(id);
        if (place == null) return NotFound();
        var vm = new CreatePlaceViewModel
        {
            Name = place.Name,
            Description = place.Description,
            Address = place.Address,
            Latitude = place.Latitude,
            Longitude = place.Longitude,
            Phone = place.Phone,
            OpenTime = place.OpenTime,
            CloseTime = place.CloseTime
        };
        ViewBag.PlaceId = id;
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CreatePlaceViewModel vm)
    {
        if (!ModelState.IsValid) { ViewBag.PlaceId = id; return View(vm); }
        var (success, error) = await api.UpdatePlaceAsync(id, vm);
        if (!success)
        {
            ModelState.AddModelError("", "Cập nhật thất bại.");
            ViewBag.PlaceId = id;
            return View(vm);
        }
        TempData["Success"] = "Cập nhật thành công!";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await api.DeletePlaceAsync(id);
        TempData["Success"] = "Đã xóa quán.";
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Map()
    {
        if (!IsLoggedIn) return RedirectToAction("Login", "Auth");
        var result = await api.GetPlacesAsync(page: 1);
        return View(result?.Items ?? []);
    }
}