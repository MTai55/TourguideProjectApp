using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourGuideAPI.Data;

namespace TourGuideAPI.Controllers;

[ApiController]
[Route("api/admin/sessions")]
[Authorize(Policy = "AdminOnly")]
public class AccessSessionsController(AppDbContext db) : ControllerBase
{
    // GET /api/admin/sessions?status=pending&page=1
    [HttpGet]
    public async Task<IActionResult> GetSessions(
        [FromQuery] string status = "pending",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null)
    {
        var query = db.AccessSessions.AsQueryable();

        query = status switch
        {
            "pending"  => query.Where(s => !s.IsActive && s.ActivatedAt == null),
            "active"   => query.Where(s => s.IsActive && s.ExpiresAt > DateTime.UtcNow),
            "expired"  => query.Where(s => s.IsActive && s.ExpiresAt <= DateTime.UtcNow),
            _          => query
        };

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(s => s.DeviceId.Contains(search));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new SessionDto
            {
                SessionId     = s.SessionId,
                DeviceId      = s.DeviceId,
                PackageId     = s.PackageId,
                DurationHours = s.DurationHours,
                PriceVnd      = s.PriceVnd,
                CreatedAt     = s.CreatedAt,
                ActivatedAt   = s.ActivatedAt,
                ExpiresAt     = s.ExpiresAt,
                IsActive      = s.IsActive,
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    // POST /api/admin/sessions/{sessionId}/activate
    [HttpPost("{sessionId}/activate")]
    public async Task<IActionResult> Activate(Guid sessionId)
    {
        var session = await db.AccessSessions.FindAsync(sessionId);
        if (session == null) return NotFound(new { message = "Session không tồn tại." });
        if (session.IsActive) return BadRequest(new { message = "Session đã được kích hoạt." });

        var now = DateTime.UtcNow;
        session.IsActive    = true;
        session.ActivatedAt = now;
        session.ExpiresAt   = now.AddHours(session.DurationHours);

        await db.SaveChangesAsync();

        return Ok(new
        {
            sessionId   = session.SessionId,
            deviceId    = session.DeviceId,
            activatedAt = session.ActivatedAt,
            expiresAt   = session.ExpiresAt,
        });
    }

    // POST /api/admin/sessions/{sessionId}/deactivate
    [HttpPost("{sessionId}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid sessionId)
    {
        var session = await db.AccessSessions.FindAsync(sessionId);
        if (session == null) return NotFound(new { message = "Session không tồn tại." });

        session.IsActive  = false;
        session.ExpiresAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(new { message = "Đã thu hồi quyền truy cập." });
    }

    // DELETE /api/admin/sessions/{sessionId}
    [HttpDelete("{sessionId}")]
    public async Task<IActionResult> Delete(Guid sessionId)
    {
        var session = await db.AccessSessions.FindAsync(sessionId);
        if (session == null) return NotFound(new { message = "Session không tồn tại." });
        if (session.IsActive) return BadRequest(new { message = "Không thể xóa session đã kích hoạt." });

        db.AccessSessions.Remove(session);
        await db.SaveChangesAsync();
        return Ok(new { message = "Đã hủy session." });
    }

    // GET /api/admin/sessions/stats
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var now = DateTime.UtcNow;
        var pending  = await db.AccessSessions.CountAsync(s => !s.IsActive && s.ActivatedAt == null);
        var active   = await db.AccessSessions.CountAsync(s => s.IsActive && s.ExpiresAt > now);
        var total    = await db.AccessSessions.CountAsync();
        var revenue  = await db.AccessSessions.Where(s => s.IsActive).SumAsync(s => (long)s.PriceVnd);

        return Ok(new { pending, active, total, revenue });
    }
}

public class SessionDto
{
    public Guid SessionId { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string PackageId { get; set; } = string.Empty;
    public double DurationHours { get; set; }
    public int PriceVnd { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; }
}
