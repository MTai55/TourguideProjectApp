using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace TourGuideAPI.Extensions;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(opt =>
        {
            // ── Policy mặc định: 60 requests/phút ────────────────────
            opt.AddFixedWindowLimiter("general", o => {
                o.PermitLimit = 60;
                o.Window = TimeSpan.FromMinutes(1);
                o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                o.QueueLimit = 5;
            });

            // ── Policy auth: 10 requests/phút (chống brute force) ────
            opt.AddFixedWindowLimiter("auth", o => {
                o.PermitLimit = 10;
                o.Window = TimeSpan.FromMinutes(1);
                o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                o.QueueLimit = 2;
            });

            // ── Policy upload: 20 requests/phút ──────────────────────
            opt.AddFixedWindowLimiter("upload", o => {
                o.PermitLimit = 20;
                o.Window = TimeSpan.FromMinutes(1);
            });

            // Trả về 429 Too Many Requests khi vượt giới hạn
            opt.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            opt.OnRejected = async (context, token) => {
                context.HttpContext.Response.StatusCode = 429;
                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    error = "Quá nhiều yêu cầu. Vui lòng thử lại sau.",
                    retryAfter = "60 giây"
                }, token);
            };
        });
        return services;
    }
}