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
        var query = db.DevicePoiVisits.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(v => v.DeviceId.Contains(search));

        var grouped = await query
            .GroupBy(v => v.DeviceId)
            .Select(g => new DeviceStatDto
            {
                DeviceId   = g.Key,
                VisitCount = g.Count(),
                PoiCount   = g.Select(v => v.PlaceId).Distinct().Count(),
                FirstVisit = g.Min(v => v.VisitedAt),
                LastVisit  = g.Max(v => v.VisitedAt),
            })
            .OrderByDescending(d => d.LastVisit)
            .ToListAsync();

        var total = grouped.Count;
        var items = grouped.Skip((page - 1) * pageSize).Take(pageSize).ToList();

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
            .Select(v => new
            {
                v.VisitId,
                v.PlaceId,
                v.PlaceName,
                v.VisitMethod,
                v.VisitedAt
            })
            .ToListAsync();

        return Ok(visits);
    }
}

public class DeviceStatDto
{
    public string DeviceId { get; set; } = string.Empty;
    public int VisitCount { get; set; }
    public int PoiCount { get; set; }
    public DateTime? FirstVisit { get; set; }
    public DateTime? LastVisit { get; set; }
}
