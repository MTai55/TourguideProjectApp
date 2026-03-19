using Microsoft.AspNetCore.Mvc;
using TourismApp.Web.Services;
using TourismApp.Web.Filters;

namespace TourismApp.Web.Controllers.Admin;


[AdminOnly]
public class AdminUsersController(ApiService api) : Controller
{
    // GET /Admin/AdminUsers
    public async Task<IActionResult> Index(string? search, string? role)
    {
        var users = await api.GetUsersAsync(search, role);
        ViewBag.Search = search;
        ViewBag.Role = role;
        return View(users ?? []);
    }

    // POST /Admin/AdminUsers/ToggleLock/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLock(int id)
    {
        await api.ToggleUserLockAsync(id);
        TempData["Success"] = "Đã cập nhật trạng thái tài khoản!";
        return RedirectToAction("Index");
    }

    // POST /Admin/AdminUsers/ChangeRole
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeRole(int id, string role)
    {
        await api.ChangeUserRoleAsync(id, role);
        TempData["Success"] = "Đã đổi quyền tài khoản!";
        return RedirectToAction("Index");
    }
}