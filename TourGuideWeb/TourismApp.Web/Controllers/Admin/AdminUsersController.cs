using Microsoft.AspNetCore.Mvc;
using TourismApp.Web.Services;
using TourismApp.Web.Filters;
using TourismApp.Web.Models;

namespace TourismApp.Web.Controllers.Admin;

[Area("Admin")]
[AdminOnly]
public class AdminUsersController(ApiService api, ILogger<AdminUsersController> logger) : Controller
{
    // GET /Admin/AdminUsers
    public async Task<IActionResult> Index(string? search, string? role)
    {
        logger.LogInformation("AdminUsersController.Index called with search={search}, role={role}", search, role);
        
        var users = await api.GetUsersAsync(search, role);
        
        logger.LogInformation("API returned {count} users", users?.Count ?? 0);
        if (users == null)
        {
            logger.LogWarning("GetUsersAsync returned null");
        }
        
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