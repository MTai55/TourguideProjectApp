using Microsoft.EntityFrameworkCore;
using TourGuideAPI.Data;
using TourGuideAPI.DTOs.Tracking;
using TourGuideAPI.Models;

namespace TourGuideAPI.Services;

public interface ITrackingService
{
    Task LogLocationAsync(int userId, LocationDto dto);
    Task<VisitHistory> CheckInAsync(int userId, CheckInDto dto);
    Task CheckOutAsync(int userId, int visitId);
    Task<TripStatsDto> GetTripStatsAsync(int userId);
    Task<List<VisitSummaryDto>> GetVisitHistoryAsync(int userId, int page = 1);
}

public class TrackingService(AppDbContext db, IGeoLocationService geo, IConfiguration cfg)
    : ITrackingService
{
    public async Task LogLocationAsync(int userId, LocationDto dto)
    {
        db.UserTracking.Add(new UserTracking
        {
            UserId = userId,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            Accuracy = dto.Accuracy
        });
        await db.SaveChangesAsync();

        // Auto check-in nếu gần địa điểm
        var radius = cfg.GetValue<double>("GeoSettings:CheckInRadiusMeters", 100);
        var place = await geo.DetectNearestPlaceAsync(dto.Latitude, dto.Longitude, radius);
        if (place != null)
        {
            var recent = await db.VisitHistory
                .Where(v => v.UserId == userId && v.PlaceId == place.PlaceId
                         && v.CheckInTime > DateTime.UtcNow.AddHours(-1))
                .AnyAsync();
            if (!recent) // tránh check-in duplicate
                await CheckInAsync(userId, new(place.PlaceId, dto.Latitude, dto.Longitude, AutoDetected: true));
        }
    }

    public async Task<VisitHistory> CheckInAsync(int userId, CheckInDto dto)
    {
        var visit = new VisitHistory
        {
            UserId = userId,
            PlaceId = dto.PlaceId,
            AutoDetected = dto.AutoDetected,
            Notes = dto.Notes
        };
        db.VisitHistory.Add(visit);
        // Tăng TotalVisits
        await db.Places.Where(p => p.PlaceId == dto.PlaceId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.TotalVisits, p => p.TotalVisits + 1));
        await db.SaveChangesAsync();
        return visit;
    }

    public async Task CheckOutAsync(int userId, int visitId)
    {
        var visit = await db.VisitHistory
            .FirstOrDefaultAsync(v => v.VisitId == visitId && v.UserId == userId);
        if (visit == null) return;
        visit.CheckOutTime = DateTime.UtcNow;
        visit.DurationMins = (int)(visit.CheckOutTime.Value - visit.CheckInTime).TotalMinutes;
        await db.SaveChangesAsync();
    }

    public async Task<TripStatsDto> GetTripStatsAsync(int userId)
    {
        var visits = await db.VisitHistory
            .Include(v => v.Place).ThenInclude(p => p!.Images)
            .Where(v => v.UserId == userId)
            .OrderByDescending(v => v.CheckInTime)
            .ToListAsync();

        var stats = new TripStatsDto(
            TotalVisits: visits.Count,
            UniquePlaces: visits.Select(v => v.PlaceId).Distinct().Count(),
            TotalDistanceKm: 0, // tính từ GPS log nếu cần
            TotalMinutesSpent: visits.Sum(v => v.DurationMins ?? 0),
            RecentVisits: visits.Take(10).Select(v => new VisitSummaryDto(
                v.VisitId, v.PlaceId, v.Place!.Name,
                v.Place.Images.FirstOrDefault(i => i.IsMain)?.ImageUrl,
                v.CheckInTime, v.DurationMins
            )).ToList()
        );
        return stats;
    }

    public async Task<List<VisitSummaryDto>> GetVisitHistoryAsync(int userId, int page = 1)
        => await db.VisitHistory
            .Include(v => v.Place).ThenInclude(p => p!.Images)
            .Where(v => v.UserId == userId)
            .OrderByDescending(v => v.CheckInTime)
            .Skip((page - 1) * 20).Take(20)
            .Select(v => new VisitSummaryDto(
                v.VisitId, v.PlaceId, v.Place!.Name,
                v.Place.Images.FirstOrDefault(i => i.IsMain)!.ImageUrl,
                v.CheckInTime, v.DurationMins))
            .ToListAsync();
}