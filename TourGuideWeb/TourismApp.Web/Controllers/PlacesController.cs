using Microsoft.AspNetCore.Mvc;
using TourismApp.Web.Models;
using TourismApp.Web.Services;
using TourismApp.Web.Filters;

namespace TourismApp.Web.Controllers;

[OwnerOnly]
public class PlacesController(ApiService api, ILogger<PlacesController> logger) : Controller
{
    private bool IsLoggedIn => HttpContext.Session.GetString("JwtToken") != null;

    // ── GET /Places/Debug ─────────────────────────────────────────
    [HttpGet("debug")]
    public IActionResult Debug()
    {
        var token = HttpContext.Session.GetString("JwtToken");
        var userName = HttpContext.Session.GetString("UserName");
        var userId = HttpContext.Session.GetInt32("UserId");
        var userRole = HttpContext.Session.GetString("UserRole");
        
        return Ok(new
        {
            message = "🔍 DEBUG INFO (Web)",
            session = new
            {
                isLoggedIn = IsLoggedIn,
                userName,
                userId,
                userRole,
                tokenLength = token?.Length ?? 0,
                tokenPrefix = token?.Substring(0, Math.Min(20, token?.Length ?? 0)) + "..."
            }
        });
    }

    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        var token = HttpContext.Session.GetString("JwtToken");
        var userName = HttpContext.Session.GetString("UserName");
        var userId = HttpContext.Session.GetInt32("UserId");
        var userRole = HttpContext.Session.GetString("UserRole");
        
        logger.LogInformation($"🔍 PlacesController.Index called");
        logger.LogInformation($"   Token: {(token != null ? "✅ Present (" + token.Length + " chars)" : "❌ Not found")}");
        logger.LogInformation($"   UserName: {userName ?? "null"}");
        logger.LogInformation($"   UserId: {userId}");
        logger.LogInformation($"   Role: {userRole ?? "null"}");
        
        // Gọi /api/places/mine — chỉ quán của owner này
        var result = await api.GetMyPlacesAsync(page, search);
        
        logger.LogInformation($"   API Result: {(result != null ? $"✅ OK ({result.Items.Count} items)" : "❌ Null")}");
        
        // Debug: nếu result null → API có lỗi
        if (result == null)
        {
            TempData["Error"] = "❌ Lỗi kết nối API. Hãy kiểm tra logs.";
        }
        
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
        // Debug: xem ModelState lỗi gì
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage);
            TempData["Error"] = "Validation: " + string.Join(", ", errors);
            return View(vm);
        }

        var (success, _, error) = await api.CreatePlaceAsync(vm);
        if (!success)
        {
            TempData["Error"] = $"API lỗi: {error}"; // ← hiện lỗi từ API
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
            CloseTime = place.CloseTime,
            CategoryId = place.CategoryId,
            Specialty = place.Specialty,
            PriceMin = place.PriceMin,
            PriceMax = place.PriceMax,
            District = place.District,
            HasParking = place.HasParking,
            HasAircon = place.HasAircon,
            PricePerPerson = place.PricePerPerson
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