using TourGuideAPP.Data.Models;

namespace TourGuideAPP.Services;

public class GeofenceEngine
{
    // Debounce: POI phải được detect liên tục trong khoảng thời gian này mới trigger
    private const int DebounceMs = 2000;

    // Theo dõi POI đang "chờ debounce"
    private string? _pendingPlaceId;
    private DateTime _pendingFirstSeenAt = DateTime.MinValue;

    // Tính khoảng cách giữa 2 tọa độ (Haversine)
    public double GetDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000;
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    /// <summary>
    /// Tìm POI ưu tiên cao nhất trong vùng, qua debounce + cooldown.
    /// Trả về null nếu chưa đủ thời gian debounce hoặc không có POI nào.
    /// </summary>
    public Place? FindNearestPOI(double userLat, double userLon, List<Place> places)
    {
        // Lọc các POI trong vùng, chưa hết cooldown (không yêu cầu TtsScript)
        var candidates = places
            .Where(p =>
                GetDistance(userLat, userLon, p.Latitude, p.Longitude) <= (p.Radius ?? 50) &&
                (p.LastPlayedAt == null ||
                 (DateTime.Now - p.LastPlayedAt.Value).TotalMinutes >= (p.CooldownMinutes ?? 30)))
            .OrderBy(p => p.PlaceId)
            .ToList();

        if (candidates.Count == 0)
        {
            // Không còn POI nào trong vùng → reset debounce
            _pendingPlaceId = null;
            _pendingFirstSeenAt = DateTime.MinValue;
            return null;
        }

        var top = candidates[0];
        var topId = top.PlaceId.ToString();

        // Debounce: nếu là POI mới → bắt đầu đếm giờ
        if (_pendingPlaceId != topId)
        {
            _pendingPlaceId = topId;
            _pendingFirstSeenAt = DateTime.Now;
            return null; // Chưa đủ debounce
        }

        // Kiểm tra đã đứng trong vùng đủ lâu chưa
        if ((DateTime.Now - _pendingFirstSeenAt).TotalMilliseconds < DebounceMs)
            return null; // Chưa đủ debounce

        // Đủ debounce → trả về POI (KHÔNG reset để tránh handler thứ 2 restart debounce)
        return top;
    }

    private static double ToRad(double deg) => deg * Math.PI / 180;
}
