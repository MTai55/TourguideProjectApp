using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using TourGuideAPI.Data;
using TourGuideAPI.DTOs.Subscriptions;
using TourGuideAPI.Models;

namespace TourGuideAPI.Controllers;

[ApiController]
[Route("api/subscriptions")]
public class SubscriptionController(AppDbContext db, IConfiguration config) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ── GET /api/subscriptions/plans — Lấy tất cả gói ───────────
    [HttpGet("plans")]
    public async Task<IActionResult> GetPlans()
    {
        var plans = await db.SubscriptionPlans
            .Where(p => p.IsActive)
            .OrderBy(p => p.Price)
            .Select(p => new SubscriptionPlanDto
            {
                PlanId       = p.PlanId,
                Name         = p.Name,
                Slug         = p.Slug,
                Price        = p.Price,
                MaxPlaces    = p.MaxPlaces,
                HasTts       = p.HasTts,
                HasAnalytics = p.HasAnalytics,
                HasPriority  = p.HasPriority,
                Features = p.Features != null
                ? JsonSerializer.Deserialize<List<string>>(p.Features) ?? new List<string>()
                : new List<string>(),
            })
            .ToListAsync();
        return Ok(plans);
    }

    // ── GET /api/subscriptions/mine — Subscription hiện tại ──────
    [HttpGet("mine")]
    [Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> GetMine()
    {
        var sub = await db.Subscriptions
            .Include(s => s.Plan)
            .Where(s => s.OwnerId == UserId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new SubscriptionDto
            {
                SubId         = s.SubId,
                PlanName      = s.Plan!.Name,
                PlanSlug      = s.Plan.Slug,
                Status        = s.Status,
                StartDate     = s.StartDate,
                EndDate       = s.EndDate,
                PaymentMethod = s.PaymentMethod,
                Amount        = s.Amount,
                IsActive      = s.Status == "Active" && s.EndDate > DateTime.UtcNow,
                DaysRemaining = s.EndDate.HasValue
                    ? Math.Max(0, (int)(s.EndDate.Value - DateTime.UtcNow).TotalDays)
                    : null,
            })
            .FirstOrDefaultAsync();

        return Ok(sub); // null = chưa đăng ký
    }

    // ── GET /api/subscriptions/history — Lịch sử thanh toán ─────
    [HttpGet("history")]
    [Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> GetHistory()
    {
        var history = await db.Subscriptions
            .Include(s => s.Plan)
            .Where(s => s.OwnerId == UserId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new SubscriptionDto
            {
                SubId         = s.SubId,
                PlanName      = s.Plan!.Name,
                PlanSlug      = s.Plan.Slug,
                Status        = s.Status,
                StartDate     = s.StartDate,
                EndDate       = s.EndDate,
                PaymentMethod = s.PaymentMethod,
                Amount        = s.Amount,
                IsActive      = s.Status == "Active" && s.EndDate > DateTime.UtcNow,
            })
            .ToListAsync();
        return Ok(history);
    }

    // ── POST /api/subscriptions — Tạo subscription + lấy payment URL
    [HttpPost]
    [Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> Create([FromBody] CreateSubscriptionDto dto)
    {
        var plan = await db.SubscriptionPlans.FindAsync(dto.PlanId);
        if (plan == null || !plan.IsActive)
            return BadRequest(new { message = "Gói không tồn tại." });

        if (!new[] { "vnpay", "momo", "stripe" }.Contains(dto.PaymentMethod))
            return BadRequest(new { message = "Phương thức thanh toán không hợp lệ." });

        // Tạo subscription Pending
        var sub = new Subscription
        {
            OwnerId       = UserId,
            PlanId        = dto.PlanId,
            Status        = "Pending",
            PaymentMethod = dto.PaymentMethod,
            Amount        = plan.Price,
        };
        db.Subscriptions.Add(sub);
        await db.SaveChangesAsync();

        // Tạo payment URL theo cổng
        var paymentUrl = dto.PaymentMethod switch
        {
            "vnpay"  => BuildVnpayUrl(sub.SubId, plan.Price),
            "momo"   => BuildMomoUrl(sub.SubId, plan.Price),
            "stripe" => BuildStripeUrl(sub.SubId, plan.Price, plan.Name),
            _        => throw new Exception("Invalid payment method")
        };

        return Ok(new {
            subId      = sub.SubId,
            paymentUrl,
            amount     = plan.Price,
            planName   = plan.Name,
            method     = dto.PaymentMethod,
        });
    }

    // ── GET /api/subscriptions/vnpay/callback ────────────────────
    [HttpGet("vnpay/callback")]
    public async Task<IActionResult> VnpayCallback([FromQuery] Dictionary<string, string> query)
    {
        // Lấy subId từ vnp_TxnRef
        if (!query.TryGetValue("vnp_TxnRef", out var txnRef) ||
            !int.TryParse(txnRef, out var subId))
            return Redirect(BuildFailUrl("Invalid transaction"));

        var sub = await db.Subscriptions.Include(s => s.Plan).FirstOrDefaultAsync(s => s.SubId == subId);
        if (sub == null) return Redirect(BuildFailUrl("Subscription not found"));

        var responseCode = query.GetValueOrDefault("vnp_ResponseCode", "99");
        var transId      = query.GetValueOrDefault("vnp_TransactionNo", "");

        if (responseCode == "00") // Thành công
        {
            await ActivateSubscription(sub, transId);
            return Redirect($"{config["App:WebUrl"]}/Subscription/Success?subId={subId}");
        }
        else
        {
            sub.Status = "Failed";
            sub.Notes  = $"VNPay error code: {responseCode}";
            await db.SaveChangesAsync();
            return Redirect(BuildFailUrl($"VNPay error {responseCode}"));
        }
    }

    // ── POST /api/subscriptions/momo/callback ────────────────────
    [HttpPost("momo/callback")]
    public async Task<IActionResult> MomoCallback([FromBody] JsonElement body)
    {
        var subId    = body.GetProperty("orderId").GetString()?.Replace("SUB_", "") ?? "";
        var resultCode = body.GetProperty("resultCode").GetInt32();
        var transId  = body.GetProperty("transId").GetString() ?? "";

        if (!int.TryParse(subId, out var id))
            return BadRequest();

        var sub = await db.Subscriptions.Include(s => s.Plan).FirstOrDefaultAsync(s => s.SubId == id);
        if (sub == null) return NotFound();

        if (resultCode == 0)
        {
            await ActivateSubscription(sub, transId);
        }
        else
        {
            sub.Status = "Failed";
            sub.Notes  = $"MoMo error code: {resultCode}";
            await db.SaveChangesAsync();
        }
        return Ok();
    }

    // ── POST /api/subscriptions/stripe/webhook ───────────────────
    [HttpPost("stripe/webhook")]
    public async Task<IActionResult> StripeWebhook()
    {
        var payload = await new StreamReader(Request.Body).ReadToEndAsync();
        var json    = JsonDocument.Parse(payload).RootElement;

        var eventType = json.GetProperty("type").GetString();
        if (eventType == "payment_intent.succeeded")
        {
            var pi     = json.GetProperty("data").GetProperty("object");
            var meta   = pi.GetProperty("metadata");
            var subId  = meta.GetProperty("subId").GetString() ?? "";
            var transId= pi.GetProperty("id").GetString() ?? "";

            if (int.TryParse(subId, out var id))
            {
                var sub = await db.Subscriptions.Include(s => s.Plan).FirstOrDefaultAsync(s => s.SubId == id);
                if (sub != null) await ActivateSubscription(sub, transId);
            }
        }
        return Ok();
    }

    // ── POST /api/subscriptions/{id}/cancel ──────────────────────
    [HttpPost("{id}/cancel")]
    [Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> Cancel(int id)
    {
        var sub = await db.Subscriptions
            .FirstOrDefaultAsync(s => s.SubId == id && s.OwnerId == UserId);
        if (sub == null) return NotFound();
        if (sub.Status != "Active")
            return BadRequest(new { message = "Chỉ có thể hủy subscription đang Active." });

        sub.Status    = "Cancelled";
        sub.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(new { message = "Đã hủy subscription. Quyền truy cập vẫn còn đến hết kỳ." });
    }

    // ── ADMIN: GET /api/subscriptions/admin ──────────────────────
    [HttpGet("admin")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> AdminGetAll(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1)
    {
        var query = db.Subscriptions
            .Include(s => s.Plan)
            .Include(s => s.Owner)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(s => s.Status == status);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * 20).Take(20)
            .Select(s => new {
                s.SubId,
                OwnerName     = s.Owner!.FullName,
                OwnerEmail    = s.Owner.Email,
                PlanName      = s.Plan!.Name,
                s.Status,
                s.StartDate,
                s.EndDate,
                s.PaymentMethod,
                s.Amount,
                s.TransactionId,
                s.CreatedAt,
            })
            .ToListAsync();

        return Ok(new { total, page, items });
    }

    // ── Helpers ───────────────────────────────────────────────────
    private async Task ActivateSubscription(Subscription sub, string transactionId)
    {
        // Hủy subscription cũ nếu có
        var oldSubs = await db.Subscriptions
            .Where(s => s.OwnerId == sub.OwnerId && s.Status == "Active" && s.SubId != sub.SubId)
            .ToListAsync();
        foreach (var old in oldSubs)
            old.Status = "Cancelled";

        sub.Status        = "Active";
        sub.TransactionId = transactionId;
        sub.StartDate     = DateTime.UtcNow;
        sub.EndDate       = DateTime.UtcNow.AddMonths(1);
        sub.UpdatedAt     = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    private string BuildVnpayUrl(int subId, int amount)
    {
        var baseUrl   = config["Payment:VNPay:Url"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
        var returnUrl = $"{config["App:ApiUrl"]}/api/subscriptions/vnpay/callback";
        var tmnCode   = config["Payment:VNPay:TmnCode"] ?? "DEMO";
        var secretKey = config["Payment:VNPay:SecretKey"] ?? "DEMO_SECRET";

        // Build params (simplified — production cần sort + HMAC-SHA512)
        return $"{baseUrl}?vnp_Amount={amount * 100}&vnp_Command=pay" +
               $"&vnp_CreateDate={DateTime.Now:yyyyMMddHHmmss}" +
               $"&vnp_CurrCode=VND&vnp_IpAddr=127.0.0.1" +
               $"&vnp_Locale=vn&vnp_OrderInfo=Pho+Am+Thuc+Sub+{subId}" +
               $"&vnp_OrderType=250000&vnp_ReturnUrl={Uri.EscapeDataString(returnUrl)}" +
               $"&vnp_TmnCode={tmnCode}&vnp_TxnRef={subId}" +
               $"&vnp_Version=2.1.0";
    }

    private string BuildMomoUrl(int subId, int amount)
    {
        // MoMo payment URL (simplified for demo)
        var returnUrl = $"{config["App:WebUrl"]}/Subscription/Success?subId={subId}";
        var notifyUrl = $"{config["App:ApiUrl"]}/api/subscriptions/momo/callback";
        return $"https://test-payment.momo.vn/v2/gateway/pay?" +
               $"orderId=SUB_{subId}&amount={amount}&orderInfo=Pho+Am+Thuc+{subId}" +
               $"&returnUrl={Uri.EscapeDataString(returnUrl)}" +
               $"&notifyUrl={Uri.EscapeDataString(notifyUrl)}";
    }

    private string BuildStripeUrl(int subId, int amount, string planName)
    {
        // Stripe Checkout Session URL (cần Stripe SDK trong production)
        var successUrl = $"{config["App:WebUrl"]}/Subscription/Success?subId={subId}";
        var cancelUrl  = $"{config["App:WebUrl"]}/Subscription/Plans";
        return $"https://checkout.stripe.com/pay/demo?subId={subId}&amount={amount}&name={Uri.EscapeDataString(planName)}&success_url={Uri.EscapeDataString(successUrl)}&cancel_url={Uri.EscapeDataString(cancelUrl)}";
    }

    private string BuildFailUrl(string reason)
        => $"{config["App:WebUrl"]}/Subscription/Failed?reason={Uri.EscapeDataString(reason)}";
}
