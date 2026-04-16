using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TourGuideAPI.DTOs.Auth;
using TourGuideAPI.Services;
using Microsoft.AspNetCore.RateLimiting;
using TourGuideAPI.Data;
using Microsoft.EntityFrameworkCore;


namespace TourGuideAPI.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public class AuthController(IAuthService auth, AppDbContext db) : ControllerBase
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
    // GET /api/auth/profile
    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await db.Users
            .Where(u => u.UserId == userId)
            .Select(u => new {
                u.UserId, u.FullName, u.Email, u.Phone,
                u.Role, u.CreatedAt, u.AvatarUrl
            })
            .FirstOrDefaultAsync();

        return user == null ? NotFound() : Ok(user);
    }

    // PUT /api/auth/profile
    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user   = await db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        // Kiểm tra email trùng (nếu đổi email)
        if (user.Email != dto.Email)
        {
            var exists = await db.Users.AnyAsync(u => u.Email == dto.Email && u.UserId != userId);
            if (exists) return BadRequest(new { message = "Email này đã được sử dụng bởi tài khoản khác." });
        }

        user.FullName = dto.FullName;
        user.Email    = dto.Email;
        user.Phone    = dto.Phone;
        await db.SaveChangesAsync();

        return Ok(new { user.UserId, user.FullName, user.Email, user.Phone, user.Role });
    }

    // PUT /api/auth/change-password
    [HttpPut("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user   = await db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        // Verify mật khẩu hiện tại
        if (string.IsNullOrEmpty(user.PasswordHash) ||
            !BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            return BadRequest(new { message = "Mật khẩu hiện tại không đúng." });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await db.SaveChangesAsync();

        return Ok(new { message = "Đổi mật khẩu thành công." });
    }
}