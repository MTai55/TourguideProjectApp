using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TourGuideAPI.Data;
using TourGuideAPI.Models;

namespace TourGuideAPI.Controllers;

[ApiController]
[Route("api/analytics")]
[Authorize(Policy = "OwnerOnly")]
public class AnalyticsController(AppDbContext db) : ControllerBase
{
    private int OwnerId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // GET /api/analytics/visits/{placeId}?days=30
    [HttpGet("visits/{placeId}")]
    public async Task<IActionResult> GetVisits(int placeId, [FromQuery] int days = 30)
    {
        var isOwner = await db.Places.AnyAsync(p => p.PlaceId == placeId && p.OwnerId == OwnerId);
        if (!isOwner) return Forbid();

        var from = DateTime.UtcNow.AddDays(-days);
        var visits = await db.VisitHistory
            .Where(v => v.PlaceId == placeId && v.CheckInTime >= from)
            .ToListAsync();

        var byDay = visits
            .GroupBy(v => v.CheckInTime.Date)
            .Select(g => new { date = g.Key.ToString("yyyy-MM-dd"), count = g.Count() })
            .OrderBy(x => x.date)
            .ToList();

        var byHour = visits
            .GroupBy(v => v.CheckInTime.Hour)
            .Select(g => new { hour = g.Key, count = g.Count() })
            .OrderBy(x => x.hour)
            .ToList();

        return Ok(new
        {
            placeId,
            totalVisits = visits.Count,
            avgDuration = visits.Where(v => v.DurationMins.HasValue).Select(v => v.DurationMins).DefaultIfEmpty(0).Average(),
            byDay,
            byHour,
            peakHour = byHour.MaxBy(x => x.count)?.hour
        });
    }

    // GET /api/analytics/dashboard  — tổng hợp tất cả địa điểm
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var places = await db.Places
            .Where(p => p.OwnerId == OwnerId)
            .Select(p => new {
                p.PlaceId,
                p.Name,
                p.TotalVisits,
                p.AverageRating,
                p.TotalReviews,
                p.IsApproved
            }).ToListAsync();

        var totalVisitsThisMonth = await db.VisitHistory
            .Where(v => db.Places.Any(p => p.PlaceId == v.PlaceId && p.OwnerId == OwnerId)
                     && v.CheckInTime >= DateTime.UtcNow.AddDays(-30))
            .CountAsync();

        return Ok(new
        {
            totalPlaces = places.Count,
            approvedPlaces = places.Count(p => p.IsApproved),
            totalVisitsThisMonth,
            avgRating = places.Any() ? places.Average(p => p.AverageRating) : 0,
            places
        });
    }
}