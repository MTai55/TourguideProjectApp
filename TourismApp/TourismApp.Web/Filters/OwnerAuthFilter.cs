using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TourismApp.Web.Filters;

public class OwnerAuthFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        var session = context.HttpContext.Session;
        var token = session.GetString("JwtToken");
        var role = session.GetString("UserRole");
        var ctrl = context.RouteData.Values["controller"]?.ToString();
        var action = context.RouteData.Values["action"]?.ToString();

        // Bỏ qua trang Login/Register
        if (ctrl == "Auth") return;

        // Chưa đăng nhập → về Login
        if (string.IsNullOrEmpty(token))
        {
            context.Result = new RedirectToActionResult("Login", "Auth", null);
            return;
        }

        // Đăng nhập nhưng không phải Owner/Admin → báo lỗi
        if (role != "Owner" && role != "Admin")
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Auth", null);
            return;
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}