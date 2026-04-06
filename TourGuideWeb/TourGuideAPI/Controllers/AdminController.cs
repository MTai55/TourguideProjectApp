using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TourGuideAPI.Data;
using TourGuideAPI.Models;

namespace TourGuideAPI.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = "AdminOnly")]
public class AdminController(AppDbContext db, ILogger<AdminController> logger) : ControllerBase
{
    // ── Users ────────────────────────────────────────────────────
    [HttpGet("test")]
    [AllowAnonymous]
    public IActionResult TestEndpoint()
    {
        logger.LogInformation("Test endpoint called");
        return Ok(new { message = "API is working" });
    }

    [HttpGet("users")]
    [AllowAnonymous]
    public async Task<IActionResult> GetUsers([FromQuery] string? search, [FromQuery] string? role)
    {
        try
        {
            logger.LogInformation("🔍 Starting GetUsers with search={search}, role={role}", search, role);

            logger.LogInformation("📦 Accessing db.Users...");
            var q = db.Users.AsQueryable();
            logger.LogInformation("✅ db.Users accessed");

            if (!string.IsNullOrEmpty(search))
            {
                logger.LogInformation("🔎 Filtering by search: {search}", search);
                q = q.Where(u => u.Email.Contains(search) || u.FullName.Contains(search));
            }
            if (!string.IsNullOrEmpty(role))
            {
                logger.LogInformation("🏷️ Filtering by role: {role}", role);
                q = q.Where(u => u.Role == role);
            }

            logger.LogInformation("⏳ Executing ToListAsync()...");
            var users = await q.Select(u => new
            {
                u.UserId,
                u.FullName,
                u.Email,
                u.Phone,
                u.Role,
                u.IsActive
            }).ToListAsync();
            logger.LogInformation("✅ Query returned {count} users", users.Count);

            // Map to result (already mapped above)
            var result = users;

            logger.LogInformation("📤 Returning {count} mapped users", result.Count);
            return Ok(result);
        }
        catch (Exception ex)
        {
            var innerEx = ex.InnerException?.Message ?? "No inner exception";
            logger.LogError($"❌ Exception Type: {ex.GetType().Name}");
            logger.LogError($"❌ Error in GetUsers: {ex.Message}");
            logger.LogError($"❌ InnerException: {innerEx}");
            logger.LogError($"❌ StackTrace: {ex.StackTrace}");
            return StatusCode(500, new { message = "Lỗi khi lấy danh sách tài khoản", error = ex.Message, inner = innerEx, type = ex.GetType().Name });
        }
    }

    [HttpPut("users/{id}/lock")]
    public async Task<IActionResult> ToggleLock(int id)
    {
        var user = await db.Users.FindAsync(id);
        if (user == null) return NotFound();
        user.IsActive = !user.IsActive;
        await db.SaveChangesAsync();
        return Ok(new { user.IsActive });
    }

    [HttpPut("users/{id}/role")]
    public async Task<IActionResult> ChangeRole(int id, [FromBody] string role)
    {
        if (role is not ("User" or "Owner" or "Admin")) return BadRequest();
        var user = await db.Users.FindAsync(id);
        if (user == null) return NotFound();
        user.Role = role;
        await db.SaveChangesAsync();
        return Ok(new { user.Role });
    }

    // ── Places (Admin duyệt) ──────────────────────────────────────
    [HttpGet("places")]
    public async Task<IActionResult> GetPlaces([FromQuery] bool pendingOnly = false)
    {
        logger.LogInformation($"📍 GetAdminPlaces called - pendingOnly: {pendingOnly}");
        
        var places = await db.Places
            .Include(p => p.Owner)
            .Include(p => p.Category)
            .Include(p => p.Images)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
        
        // Filter if needed
        if (pendingOnly)
        {
            places = places.Where(p => p.Status == "Pending").ToList();
            logger.LogInformation($"🔄 Filtered to pending only: {places.Count} places");
        }
        else
        {
            logger.LogInformation($"📊 Total places in system: {places.Count}");
        }
        
        // Map to PlaceViewModel
        var result = places.Select(p => new
        {
            p.PlaceId,
            p.Name,
            p.Description,
            p.Address,
            p.Latitude,
            p.Longitude,
            p.Phone,
            p.OpenTime,
            p.CloseTime,
            p.AverageRating,
            p.TotalReviews,
            p.TotalVisits,
            CategoryId = p.CategoryId,
            CategoryName = p.Category?.Name,
            MainImageUrl = p.Images?.FirstOrDefault(i => i.IsMain)?.ImageUrl,
            p.IsApproved,
            p.Specialty,
            p.PricePerPerson,
            p.PriceMin,
            p.PriceMax,
            p.District,
            p.HasParking,
            p.HasAircon,
            p.Status,
            p.OpenStatus,
            OwnerName = p.Owner?.FullName
        }).ToList();
        
        logger.LogInformation($"✅ Returning {result.Count} places");
        return Ok(result);
    }

    [HttpPut("places/{id}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        await db.Places.Where(p => p.PlaceId == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.Status, "Active")
                .SetProperty(p => p.IsApproved, true));
        return Ok(new { approved = true });
    }

    [HttpPut("places/{id}/suspend")]
    public async Task<IActionResult> Suspend(int id, [FromBody] string reason)
    {
        await db.Places.Where(p => p.PlaceId == id)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, "Suspended"));
        return Ok(new { suspended = true, reason });
    }

    // ── Reviews (Admin ẩn/hiện) ───────────────────────────────────
    [HttpGet("reviews")]
    public async Task<IActionResult> GetReviews([FromQuery] bool hiddenOnly = false)
    {
        var q = db.Reviews.Include(r => r.User).Include(r => r.Place).AsQueryable();
        if (hiddenOnly) q = q.Where(r => r.IsHidden);
        return Ok(await q.OrderByDescending(r => r.CreatedAt).Take(100).ToListAsync());
    }

    [HttpPut("reviews/{id}/hide")]
    public async Task<IActionResult> HideReview(int id, [FromBody] string note)
    {
        await db.Reviews.Where(r => r.ReviewId == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.IsHidden, true)
                .SetProperty(r => r.HiddenNote, note));
        return Ok(new { hidden = true });
    }

    [HttpPut("reviews/{id}/show")]
    public async Task<IActionResult> ShowReview(int id)
    {
        await db.Reviews.Where(r => r.ReviewId == id)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.IsHidden, false));
        return Ok(new { shown = true });
    }
}