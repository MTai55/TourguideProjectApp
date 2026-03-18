using Microsoft.AspNetCore.Mvc;
using TourismApp.Web.Models;
using TourismApp.Web.Services;

namespace TourismApp.Web.Controllers;

public class PlacesController(ApiService api) : Controller
{
    private bool IsLoggedIn => HttpContext.Session.GetString("JwtToken") != null;

    // GET /Places
    public async Task<IActionResult> Index(
    string? search, int page = 1,
    string? categoryId = null,
    string? isApproved = null,
    string? sortBy = null)
    {
        if (!IsLoggedIn) return RedirectToAction("Login", "Auth");

        var result = await api.GetPlacesAsync(page, search, categoryId, isApproved, sortBy);

        ViewBag.Search = search;
        ViewBag.Page = page;
        ViewBag.Total = result?.Total ?? 0;
        ViewBag.CategoryId = categoryId;
        ViewBag.IsApproved = isApproved;
        ViewBag.SortBy = sortBy;

        return View(result?.Items ?? []);
    }

    // GET /Places/Create
    public IActionResult Create()
    {
        if (!IsLoggedIn) return RedirectToAction("Login", "Auth");
        return View(new CreatePlaceViewModel());
    }

    // POST /Places/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreatePlaceViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var (success, _, error) = await api.CreatePlaceAsync(vm);
        if (!success)
        {
            ModelState.AddModelError("", "Không thể tạo địa điểm. Vui lòng thử lại.");
            return View(vm);
        }
        TempData["Success"] = "Tạo địa điểm thành công!";
        return RedirectToAction("Index");
    }

    // GET /Places/Edit/5
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

    // POST /Places/Edit/5
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

    // POST /Places/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await api.DeletePlaceAsync(id);
        TempData["Success"] = "Đã xóa địa điểm.";
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Map()
    {
        if (!IsLoggedIn) return RedirectToAction("Login", "Auth");
        var result = await api.GetPlacesAsync(page: 1);
        return View(result?.Items ?? []);
    }
}