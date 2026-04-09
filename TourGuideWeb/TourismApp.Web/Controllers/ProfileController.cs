using Microsoft.AspNetCore.Mvc;
using TourismApp.Web.Filters;
using TourismApp.Web.Models;
using TourismApp.Web.Services;

namespace TourismApp.Web.Controllers;

[OwnerOnly]
public class ProfileController(ApiService api) : Controller
{
    // GET /Profile
    public async Task<IActionResult> Index()
    {
        var profile = await api.GetProfileAsync();
        if (profile == null)
        {
            TempData["Error"] = "Không thể tải thông tin tài khoản.";
            return RedirectToAction("Index", "Dashboard");
        }
        ViewData["Title"] = "Tài khoản của tôi";
        return View(profile);
    }

    // GET /Profile/Edit
    public async Task<IActionResult> Edit()
    {
        var profile = await api.GetProfileAsync();
        if (profile == null) return RedirectToAction("Index");

        ViewData["Title"] = "Chỉnh sửa tài khoản";
        return View(new UpdateProfileViewModel
        {
            FullName = profile.FullName,
            Email    = profile.Email,
            Phone    = profile.Phone,
        });
    }

    // POST /Profile/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UpdateProfileViewModel vm)
    {
        if (!ModelState.IsValid) { ViewData["Title"] = "Chỉnh sửa tài khoản"; return View(vm); }

        var (ok, error) = await api.UpdateProfileAsync(vm);
        if (!ok)
        {
            ModelState.AddModelError("", error ?? "Cập nhật thất bại.");
            ViewData["Title"] = "Chỉnh sửa tài khoản";
            return View(vm);
        }

        // Cập nhật tên trong Session
        HttpContext.Session.SetString("UserName", vm.FullName);
        TempData["Success"] = "Đã cập nhật thông tin tài khoản!";
        return RedirectToAction("Index");
    }

    // GET /Profile/ChangePassword
    public IActionResult ChangePassword()
    {
        ViewData["Title"] = "Đổi mật khẩu";
        return View(new ChangePasswordViewModel());
    }

    // POST /Profile/ChangePassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel vm)
    {
        if (!ModelState.IsValid) { ViewData["Title"] = "Đổi mật khẩu"; return View(vm); }

        var (ok, error) = await api.ChangePasswordAsync(vm);
        if (!ok)
        {
            ModelState.AddModelError("", error ?? "Đổi mật khẩu thất bại. Kiểm tra lại mật khẩu hiện tại.");
            ViewData["Title"] = "Đổi mật khẩu";
            return View(vm);
        }

        TempData["Success"] = "Đã đổi mật khẩu thành công!";
        return RedirectToAction("Index");
    }
}
