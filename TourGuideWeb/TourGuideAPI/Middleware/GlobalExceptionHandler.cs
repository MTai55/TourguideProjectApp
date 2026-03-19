using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace TourGuideAPI.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken ct)
    {
        _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        var (status, title) = exception switch
        {
            UnauthorizedAccessException => (401, "Không có quyền truy cập"),
            KeyNotFoundException => (404, "Không tìm thấy tài nguyên"),
            ArgumentException => (400, "Dữ liệu không hợp lệ"),
            InvalidOperationException => (409, "Thao tác không hợp lệ"),
            _ => (500, "Lỗi hệ thống")
        };

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = exception.Message,
            Instance = context.Request.Path
        }, ct);

        return true;
    }
}