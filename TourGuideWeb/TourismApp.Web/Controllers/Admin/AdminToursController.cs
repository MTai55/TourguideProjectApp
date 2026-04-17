using Microsoft.AspNetCore.Mvc;
using TourismApp.Web.Models;
using TourismApp.Web.Filters;
using TourismApp.Web.Services;

namespace TourismApp.Web.Controllers.Admin;

[Area("Admin")]
[AdminOnly]
public class AdminToursController(ApiService api, ILogger<AdminToursController> logger) : Controller
{
    // In-memory storage cho tours (tạm bợ, không lưu database)
    private static readonly List<TourViewModel> _tours = new()
    {
        new TourViewModel
        {
            Id = "tour-1",
            Title = "Foodie Khánh Hội",
            Description = "Đi bộ nhẹ nhàng, ăn ngon, nhiều điểm check-in.",
            DurationText = "2–3 giờ",
            BudgetText = "100k–250k",
            Tag = "Ăn vặt",
            StopsText = "• 3 điểm",
            StopPlaceIds = new List<int> { 1, 2, 3 }
        },
        new TourViewModel
        {
            Id = "tour-2",
            Title = "Cafe & Chill",
            Description = "Ưu tiên quán đẹp – yên tĩnh, ít di chuyển.",
            DurationText = "2 giờ",
            BudgetText = "80k–200k",
            Tag = "Cafe",
            StopsText = "• 2 điểm",
            StopPlaceIds = new List<int> { 4, 5 }
        }
    };

    // Danh sách tours
    public IActionResult Index()
    {
        return View(_tours);
    }

    // Tạo tour mới
    public async Task<IActionResult> Create()
    {
        var places = await api.GetAdminPlacesAsync(pendingOnly: false);
        ViewBag.Places = places ?? new List<PlaceViewModel>();
        return View(new CreateTourViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateTourViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var places = await api.GetAdminPlacesAsync(pendingOnly: false);
            ViewBag.Places = places ?? new List<PlaceViewModel>();
            return View(model);
        }

        try
        {
            // Tạo tour mới
            var tour = new TourViewModel
            {
                Id = $"tour-{DateTime.Now.Ticks}",
                Title = model.Title,
                Description = model.Description,
                DurationText = model.DurationText,
                BudgetText = model.BudgetText,
                Tag = model.Tag,
                StopsText = $"• {model.StopPlaceIds.Count} điểm",
                StopPlaceIds = model.StopPlaceIds
            };

            // Load thông tin places cho stops
            var places = await api.GetAdminPlacesAsync(pendingOnly: false);
            if (places != null)
            {
                tour.Stops = places.Where(p => model.StopPlaceIds.Contains(p.PlaceId)).ToList();
            }

            _tours.Add(tour);

            TempData["Success"] = "Đã tạo tour thành công!";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            logger.LogError($"❌ Error creating tour: {ex.Message}");
            ModelState.AddModelError("", "Lỗi khi tạo tour.");
            var places = await api.GetAdminPlacesAsync(pendingOnly: false);
            ViewBag.Places = places ?? new List<PlaceViewModel>();
            return View(model);
        }
    }

    // Chỉnh sửa tour
    public async Task<IActionResult> Edit(string id)
    {
        var tour = _tours.FirstOrDefault(t => t.Id == id);
        if (tour == null)
        {
            TempData["Error"] = "Không tìm thấy tour.";
            return RedirectToAction("Index");
        }

        var places = await api.GetAdminPlacesAsync(pendingOnly: false);
        ViewBag.Places = places ?? new List<PlaceViewModel>();

        var model = new CreateTourViewModel
        {
            Title = tour.Title,
            Description = tour.Description,
            DurationText = tour.DurationText,
            BudgetText = tour.BudgetText,
            Tag = tour.Tag,
            StopPlaceIds = tour.StopPlaceIds
        };

        ViewBag.TourId = id;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, CreateTourViewModel model)
    {
        var tour = _tours.FirstOrDefault(t => t.Id == id);
        if (tour == null)
        {
            TempData["Error"] = "Không tìm thấy tour.";
            return RedirectToAction("Index");
        }

        if (!ModelState.IsValid)
        {
            var places = await api.GetAdminPlacesAsync(pendingOnly: false);
            ViewBag.Places = places ?? new List<PlaceViewModel>();
            ViewBag.TourId = id;
            return View(model);
        }

        try
        {
            // Cập nhật tour
            tour.Title = model.Title;
            tour.Description = model.Description;
            tour.DurationText = model.DurationText;
            tour.BudgetText = model.BudgetText;
            tour.Tag = model.Tag;
            tour.StopsText = $"• {model.StopPlaceIds.Count} điểm";
            tour.StopPlaceIds = model.StopPlaceIds;

            // Load lại thông tin places
            var places = await api.GetAdminPlacesAsync(pendingOnly: false);
            if (places != null)
            {
                tour.Stops = places.Where(p => model.StopPlaceIds.Contains(p.PlaceId)).ToList();
            }

            TempData["Success"] = "Đã cập nhật tour thành công!";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            logger.LogError($"❌ Error updating tour: {ex.Message}");
            ModelState.AddModelError("", "Lỗi khi cập nhật tour.");
            var places = await api.GetAdminPlacesAsync(pendingOnly: false);
            ViewBag.Places = places ?? new List<PlaceViewModel>();
            ViewBag.TourId = id;
            return View(model);
        }
    }

    // Xóa tour
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(string id)
    {
        var tour = _tours.FirstOrDefault(t => t.Id == id);
        if (tour == null)
        {
            TempData["Error"] = "Không tìm thấy tour.";
            return RedirectToAction("Index");
        }

        _tours.Remove(tour);
        TempData["Success"] = "Đã xóa tour thành công!";
        return RedirectToAction("Index");
    }

    // Xem chi tiết tour
    public async Task<IActionResult> Detail(string id)
    {
        var tour = _tours.FirstOrDefault(t => t.Id == id);
        if (tour == null)
        {
            TempData["Error"] = "Không tìm thấy tour.";
            return RedirectToAction("Index");
        }

        // Load thông tin places nếu chưa có
        if (tour.Stops.Count == 0 && tour.StopPlaceIds.Count > 0)
        {
            var places = await api.GetAdminPlacesAsync(pendingOnly: false);
            if (places != null)
            {
                tour.Stops = places.Where(p => tour.StopPlaceIds.Contains(p.PlaceId)).ToList();
            }
        }

        return View(tour);
    }

    // Sync tours to mobile app API
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncToMobile()
    {
        try
        {
            // Convert tours to data format
            var tourDataList = _tours.Select(t => new
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                DurationText = t.DurationText,
                BudgetText = t.BudgetText,
                Tag = t.Tag,
                StopPlaceIds = t.StopPlaceIds
            }).ToList();

            // Call API to sync
            var response = await api.PostAsync("/api/tours/sync", tourDataList);

            if (response)
            {
                TempData["Success"] = "Đã đồng bộ tours sang mobile app thành công!";
            }
            else
            {
                TempData["Error"] = "Lỗi khi đồng bộ tours sang mobile app.";
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"❌ Error syncing tours to mobile: {ex.Message}");
            TempData["Error"] = "Lỗi khi đồng bộ tours sang mobile app.";
        }

        return RedirectToAction("Index");
    }
}