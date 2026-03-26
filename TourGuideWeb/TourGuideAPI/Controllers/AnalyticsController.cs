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

    // GET /api/analytics/dashboard — Owner: chỉ quán của mình
    [HttpGet("dashboard")]
    [Authorize(Policy = "OwnerOnly")]
    public async Task<IActionResult> GetDashboard()
    {
        var ownerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var places = await db.Places
            .Where(p => p.OwnerId == ownerId)  // ← Chỉ quán của owner này
            .Select(p => new {
                p.PlaceId,
                p.Name,
                p.Status,
                p.OpenStatus,
                p.TotalVisits,
                p.AverageRating,
                p.TotalReviews
            }).ToListAsync();

        var placeIds = places.Select(p => p.PlaceId).ToList();

        var visitsThisMonth = await db.VisitHistory
            .Where(v => placeIds.Contains(v.PlaceId) &&
                        v.CheckInTime >= DateTime.UtcNow.AddDays(-30))
            .CountAsync();

        var pendingReviews = await db.Reviews
            .Where(r => placeIds.Contains(r.PlaceId) && r.OwnerReply == null)
            .CountAsync();

        var activePromos = await db.Promotions
            .Where(pr => placeIds.Contains(pr.PlaceId) &&
                         pr.IsActive && pr.EndDate > DateTime.UtcNow)
            .CountAsync();

        return Ok(new
        {
            TotalPlaces = places.Count,
            ActivePlaces = places.Count(p => p.Status == "Active"),
            TotalVisits = visitsThisMonth,
            PendingReviews = pendingReviews,
            ActivePromotions = activePromos,
            AvgRating = places.Any() ? places.Average(p => p.AverageRating) : 0,
            Places = places
        });
    }

    // GET /api/analytics/admin/stats — Admin: toàn hệ thống
    [HttpGet("admin/stats")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GetAdminStats()
    {
        return Ok(new
        {
            TotalUsers = await db.Users.CountAsync(),
            TotalOwners = await db.Users.CountAsync(u => u.Role == "Owner"),
            TotalPlaces = await db.Places.CountAsync(p => p.IsActive),
            PendingPlaces = await db.Places.CountAsync(p => p.Status == "Pending"),
            ActivePlaces = await db.Places.CountAsync(p => p.Status == "Active"),
            TotalReviews = await db.Reviews.CountAsync(),
            HiddenReviews = await db.Reviews.CountAsync(r => r.IsHidden),
            PendingComplaints = await db.Complaints.CountAsync(c => c.Status == "Pending"),
            TotalVisitsToday = await db.VisitHistory
                .CountAsync(v => v.CheckInTime >= DateTime.UtcNow.Date),
            AvgRating = await db.Places
                .Where(p => p.Status == "Active")
                .AverageAsync(p => p.AverageRating)
        });
    }
}