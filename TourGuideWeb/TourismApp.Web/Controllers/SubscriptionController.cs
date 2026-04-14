using Microsoft.AspNetCore.Mvc;
using TourismApp.Web.Filters;
using TourismApp.Web.Models;
using TourismApp.Web.Services;

namespace TourismApp.Web.Controllers;

[OwnerOnly]
public class SubscriptionController(ApiService api) : Controller
{
    // GET /Subscription/Plans
    public async Task<IActionResult> Plans()
    {
        ViewData["Title"] = "Chọn gói dịch vụ";
        var plans  = await api.GetSubscriptionPlansAsync();
        var mySub  = await api.GetMySubscriptionAsync();
        ViewBag.Plans      = plans ?? [];
        ViewBag.CurrentSub = mySub;
        return View(plans ?? []);
    }

    // POST /Subscription/Checkout — Tạo sub và redirect đến cổng TT
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(int planId, string paymentMethod)
    {
        var (ok, paymentUrl, error) = await api.CreateSubscriptionAsync(planId, paymentMethod);
        if (!ok || string.IsNullOrEmpty(paymentUrl))
        {
            TempData["Error"] = error ?? "Không thể tạo đơn thanh toán.";
            return RedirectToAction("Plans");
        }
        return Redirect(paymentUrl);
    }

    // GET /Subscription/Success
    public async Task<IActionResult> Success(int subId)
    {
        ViewData["Title"] = "Thanh toán thành công";
        var history = await api.GetSubscriptionHistoryAsync();
        var sub     = history?.FirstOrDefault(s => s.SubId == subId);
        ViewBag.Sub = sub;
        return View();
    }

    // GET /Subscription/Failed
    public IActionResult Failed(string? reason)
    {
        ViewData["Title"] = "Thanh toán thất bại";
        ViewBag.Reason = reason ?? "Giao dịch không thành công.";
        return View();
    }

    // GET /Subscription/History
    public async Task<IActionResult> History()
    {
        ViewData["Title"] = "Lịch sử thanh toán";
        var history = await api.GetSubscriptionHistoryAsync();
        return View(history ?? []);
    }

    // POST /Subscription/Cancel
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int subId)
    {
        var (ok, err) = await api.CancelSubscriptionAsync(subId);
        TempData[ok ? "Success" : "Error"] = ok
            ? "Đã hủy subscription. Quyền truy cập vẫn còn đến hết kỳ."
            : $"Lỗi: {err}";
        return RedirectToAction("History");
    }
}