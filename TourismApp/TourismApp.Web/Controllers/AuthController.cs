using Microsoft.AspNetCore.Mvc;
using TourismApp.Web.Models;
using TourismApp.Web.Services;

namespace TourismApp.Web.Controllers;

public class AuthController(ApiService api) : Controller
{
    // GET /Auth/Login
    public IActionResult Login() =>
        HttpContext.Session.GetString("JwtToken") != null
            ? RedirectToAction("Index", "Dashboard")
            : View();

    // POST /Auth/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var (success, data, error) = await api.LoginAsync(vm.Email, vm.Password);
        if (!success)
        {
            ModelState.AddModelError("", "Email hoặc mật khẩu không đúng.");
            return View(vm);
        }

        // Lưu token và thông tin vào Session
        HttpContext.Session.SetString("JwtToken", data!.Token);
        HttpContext.Session.SetString("UserName", data.FullName);
        HttpContext.Session.SetString("UserRole", data.Role);
        HttpContext.Session.SetInt32("UserId", data.UserId);

        return RedirectToAction("Index", "Dashboard");
    }

    // GET /Auth/Register
    public IActionResult Register() => View();

    // POST /Auth/Register
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var (success, data, error) = await api.RegisterAsync(vm);
        if (!success)
        {
            ModelState.AddModelError("", "Email đã được sử dụng.");
            return View(vm);
        }

        HttpContext.Session.SetString("JwtToken", data!.Token);
        HttpContext.Session.SetString("UserName", data.FullName);
        HttpContext.Session.SetString("UserRole", data.Role);
        HttpContext.Session.SetInt32("UserId", data.UserId);

        return RedirectToAction("Index", "Dashboard");
    }

    // GET /Auth/Logout
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }
}