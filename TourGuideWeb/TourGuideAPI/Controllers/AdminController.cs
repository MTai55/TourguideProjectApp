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
public class AdminController(AppDbContext db) : ControllerBase
{
    // ── Users ────────────────────────────────────────────────────
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] string? search, [FromQuery] string? role)
    {
        var q = db.Users.AsQueryable();
        if (!string.IsNullOrEmpty(search))
            q = q.Where(u => u.Email.Contains(search) || u.FullName.Contains(search));
        if (!string.IsNullOrEmpty(role))
            q = q.Where(u => u.Role == role);
        return Ok(await q.OrderBy(u => u.CreatedAt).ToListAsync());
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
        var q = db.Places.Include(p => p.Owner).Include(p => p.Category).AsQueryable();
        if (pendingOnly) q = q.Where(p => p.Status == "Pending");
        return Ok(await q.OrderByDescending(p => p.CreatedAt).ToListAsync());
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