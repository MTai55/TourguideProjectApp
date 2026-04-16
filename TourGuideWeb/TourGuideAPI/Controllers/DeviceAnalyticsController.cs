using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourGuideAPI.Data;

namespace TourGuideAPI.Controllers;

[ApiController]
[Route("api/admin/devices")]
[Authorize(Policy = "AdminOnly")]
public class DeviceAnalyticsController(AppDbContext db) : ControllerBase
{
    // GET /api/admin/devices?page=1&pageSize=20&search=ABCD
    [HttpGet]
    public async Task<IActionResult> GetDeviceStats(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null)
    {
        // Nguồn gốc: tất cả thiết bị đã mở app (DeviceRegistrations)
        var regQuery = db.DeviceRegistrations.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            regQuery = regQuery.Where(d => d.DeviceId.Contains(search));

        var registrations = await regQuery
            .OrderByDescending(d => d.LastSeenAt)
            .ToListAsync();

        var total = registrations.Count;
        var paged = registrations.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        // Lấy visit stats cho các device trong trang này
        var deviceIds = paged.Select(d => d.DeviceId).ToList();

        var visitStats = await db.DevicePoiVisits
            .Where(v => deviceIds.Contains(v.DeviceId))
            .GroupBy(v => v.DeviceId)
            .Select(g => new
            {
                DeviceId   = g.Key,
                VisitCount = g.Count(),
                PoiCount   = g.Select(v => v.PlaceId).Distinct().Count(),
                FirstVisit = g.Min(v => v.VisitedAt),
                LastVisit  = g.Max(v => v.VisitedAt),
            })
            .ToDictionaryAsync(g => g.DeviceId);

        // Lấy session info (có đang active không)
        var sessionStats = await db.AccessSessions
            .Where(s => deviceIds.Contains(s.DeviceId))
            .GroupBy(s => s.DeviceId)
            .Select(g => new
            {
                DeviceId  = g.Key,
                HasActive = g.Any(s => s.IsActive && s.ExpiresAt > DateTime.UtcNow),
                LastPkg   = g.OrderByDescending(s => s.CreatedAt).Select(s => s.PackageId).FirstOrDefault(),
            })
            .ToDictionaryAsync(g => g.DeviceId);

        var items = paged.Select(d =>
        {
            visitStats.TryGetValue(d.DeviceId, out var v);
            sessionStats.TryGetValue(d.DeviceId, out var s);
            return new DeviceStatDto
            {
                DeviceId    = d.DeviceId,
                Platform    = d.Platform,
                FirstSeenAt = d.FirstSeenAt,
                LastSeenAt  = d.LastSeenAt,
                VisitCount  = v?.VisitCount ?? 0,
                PoiCount    = v?.PoiCount ?? 0,
                FirstVisit  = v?.FirstVisit,
                LastVisit   = v?.LastVisit,
                HasActive   = s?.HasActive ?? false,
                LastPackage = s?.LastPkg,
            };
        }).ToList();

        return Ok(new { total, page, pageSize, items });
    }

    // GET /api/admin/devices/{deviceId}/visits
    [HttpGet("{deviceId}/visits")]
    public async Task<IActionResult> GetDeviceVisitHistory(string deviceId, [FromQuery] int limit = 50)
    {
        var visits = await db.DevicePoiVisits
            .Where(v => v.DeviceId == deviceId)
            .OrderByDescending(v => v.VisitedAt)
            .Take(limit)
            .Select(v => new { v.VisitId, v.PlaceId, v.PlaceName, v.VisitMethod, v.VisitedAt })
            .ToListAsync();

        return Ok(visits);
    }
}

public class DeviceStatDto
{
    public string DeviceId { get; set; } = string.Empty;
    public string? Platform { get; set; }
    public DateTime? FirstSeenAt { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public int VisitCount { get; set; }
    public int PoiCount { get; set; }
    public DateTime? FirstVisit { get; set; }
    public DateTime? LastVisit { get; set; }
    public bool HasActive { get; set; }
    public string? LastPackage { get; set; }
}
