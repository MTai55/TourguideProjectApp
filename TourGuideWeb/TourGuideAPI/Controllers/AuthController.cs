using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TourGuideAPI.DTOs.Auth;
using TourGuideAPI.Services;
using Microsoft.AspNetCore.RateLimiting;

namespace TourGuideAPI.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public class AuthController(IAuthService auth) : ControllerBase
{
    // POST /api/auth/register
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var result = await auth.RegisterAsync(dto);
        if (result == null)
            return Conflict(new { message = "Email đã được sử dụng." });
        return Ok(result);
    }

    // POST /api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var result = await auth.LoginAsync(dto);
        if (result == null)
            return Unauthorized(new { message = "Email hoặc mật khẩu không đúng." });
        return Ok(result); ;
    }

    // DEBUG: POST /api/auth/debug-login/{userId}
    // Purpose: Login as any user without password (for testing only)
    [HttpPost("debug-login/{userId}")]
    public async Task<IActionResult> DebugLogin(int userId, [FromServices] IAuthService authService)
    {
        try
        {
            var result = await authService.GenerateTokenAsync(userId);
            if (result == null)
                return NotFound(new { message = $"User ID {userId} not found or inactive" });
            return Ok(new
            {
                message = "🔐 DEBUG LOGIN (no password required)",
                token = result
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // POST /api/auth/refresh
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto dto)
    {
        var result = await auth.RefreshAsync(dto.RefreshToken);
        if (result == null)
            return Unauthorized(new { message = "Refresh token không hợp lệ hoặc đã hết hạn." });
        return Ok(result);
    }

    // POST /api/auth/revoke  [Authorize]
    [HttpPost("revoke")]
    [Authorize]
    public async Task<IActionResult> Revoke([FromBody] RefreshTokenRequestDto dto)
    {
        await auth.RevokeAsync(dto.RefreshToken);
        return Ok(new { revoked = true });
    }

    // GET /api/auth/me  (yêu cầu đăng nhập)
    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);
        var role = User.FindFirstValue(ClaimTypes.Role);
        var fullName = User.FindFirstValue("fullName");
        return Ok(new { userId, email, role, fullName });
    }
}