using Microsoft.AspNetCore.Mvc;
using TourismApp.Web.Models;
using TourismApp.Web.Services;
using TourismApp.Web.Filters;

namespace TourismApp.Web.Controllers;

[OwnerOnly]
public class PlacesController(ApiService api) : Controller
{
    private bool IsLoggedIn => HttpContext.Session.GetString("JwtToken") != null;

    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        // Gọi /api/places/mine — chỉ quán của owner này
        var result = await api.GetMyPlacesAsync(page, search);
        ViewBag.Search = search;
        ViewBag.Page = page;
        ViewBag.Total = result?.Total ?? 0;
        return View(result?.Items ?? []);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateOpenStatus(int id, string openStatus)
    {
        await api.UpdateOpenStatusAsync(id, openStatus);
        TempData["Success"] = "Đã cập nhật trạng thái!";
        return RedirectToAction("Index");
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
        var role = HttpContext.Session.GetString("UserRole");
        List<PlaceViewModel> places;

        if (role == "Admin")
        {
            // Admin: tất cả quán
            var result = await api.GetPlacesAsync(page: 1, pageSize: 200);
            places = result?.Items ?? [];
        }
        else
        {
            // Owner: chỉ quán của mình
            var result = await api.GetMyPlacesAsync(page: 1);
            places = result?.Items ?? [];
        }

        ViewBag.IsAdmin = role == "Admin";
        return View(places);
    }
}