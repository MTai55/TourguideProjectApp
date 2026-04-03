using Microsoft.EntityFrameworkCore;
using TourGuideAPI.Data;
using TourGuideAPI.DTOs.Places;
using TourGuideAPI.Models;

namespace TourGuideAPI.Services;

public interface IGeoLocationService
{
    Task<List<PlaceDto>> GetNearbyAsync(NearbyQueryDto query);
    double CalcDistanceKm(double lat1, double lng1, double lat2, double lng2);
    Task<Place?> DetectNearestPlaceAsync(double lat, double lng, double radiusMeters);
}

public class GeoLocationService(AppDbContext db, IConfiguration config) : IGeoLocationService
{
    public async Task<List<PlaceDto>> GetNearbyAsync(NearbyQueryDto q)
    {
        var maxRadius = config.GetValue<double>("GeoSettings:MaxRadiusKm", 50.0);
        var radius = Math.Min(q.RadiusKm, maxRadius);

        // Lấy bounding box để SQL filter trước (tối ưu hiệu suất)
        var latDelta = radius / 111.0;
        var lngDelta = radius / (111.0 * Math.Cos(q.Lat * Math.PI / 180));

        var candidates = await db.Places
            .Include(p => p.Category)
            .Include(p => p.Images.Where(i => i.IsMain))
            .Where(p => p.IsApproved && p.IsActive
                && p.Latitude >= q.Lat - latDelta && p.Latitude <= q.Lat + latDelta
                && p.Longitude >= q.Lng - lngDelta && p.Longitude <= q.Lng + lngDelta
                && (!q.CategoryId.HasValue || p.CategoryId == q.CategoryId))
            .ToListAsync();

        // Tính khoảng cách chính xác bằng Haversine và filter
        var result = candidates
            .Select(p => (Place: p, Dist: CalcDistanceKm(q.Lat, q.Lng, p.Latitude, p.Longitude)))
            .Where(x => x.Dist <= radius)
            .OrderBy(x => x.Dist)
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .Select(x => ToDto(x.Place, x.Dist))
            .ToList();

        return result;
    }

    public async Task<Place?> DetectNearestPlaceAsync(double lat, double lng, double radiusMeters)
    {
        var radiusKm = radiusMeters / 1000.0;
        var q = new NearbyQueryDto(lat, lng, radiusKm, PageSize: 1);
        var nearby = await GetNearbyAsync(q);
        if (!nearby.Any()) return null;
        return await db.Places.FindAsync(nearby[0].PlaceId);
    }

    // ── Haversine Formula ─────────────────────────────────────────
    public double CalcDistanceKm(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6371.0;
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLng = (lng2 - lng1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180)
              * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static PlaceDto ToDto(Place p, double distKm) => new(
        p.PlaceId, p.Name, p.Description, p.Address,
        p.Latitude, p.Longitude, p.Phone,
        p.OpenTime?.ToString("HH:mm"), p.CloseTime?.ToString("HH:mm"),
        p.AverageRating, p.TotalReviews, p.TotalVisits,
        p.Category?.Name,
        p.Images.FirstOrDefault(i => i.IsMain)?.ImageUrl,
        Math.Round(distKm, 2),
        p.Specialty,       
        p.PricePerPerson,  
        p.PriceMin,
        p.PriceMax,
        p.District,        
        p.HasParking,      
        p.HasAircon       
    );
}