using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TourismApp.Web.Filters;

public class AdminOnlyAttribute : TypeFilterAttribute
{
    public AdminOnlyAttribute() : base(typeof(AdminOnlyFilter)) { }
}

public class AdminOnlyFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        var role = context.HttpContext.Session.GetString("UserRole");
        var token = context.HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
            context.Result = new RedirectToActionResult("Login", "Auth", new { area = "" });
        else if (role != "Admin")
            context.Result = new RedirectToActionResult("AccessDenied", "Auth", new { area = "" });
    }
    public void OnActionExecuted(ActionExecutedContext context) { }
}

public class OwnerOnlyAttribute : TypeFilterAttribute
{
    public OwnerOnlyAttribute() : base(typeof(OwnerOnlyFilter)) { }
}

public class OwnerOnlyFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        var role = context.HttpContext.Session.GetString("UserRole");
        var token = context.HttpContext.Session.GetString("JwtToken");
        if (string.IsNullOrEmpty(token))
            context.Result = new RedirectToActionResult("Login", "Auth", null);
        else if (role != "Owner" && role != "Admin")
            context.Result = new RedirectToActionResult("AccessDenied", "Auth", null);
    }
    public void OnActionExecuted(ActionExecutedContext context) { }
}