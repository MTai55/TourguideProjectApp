using TourGuideAPP.Data.Models;


namespace TourGuideAPP.Services;

public class GeofenceEngine
{
    // Tính khoảng cách giữa 2 tọa độ (Haversine)
    public double GetDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000; // Bán kính Trái Đất (mét)
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    // Tìm địa điểm gần nhất trong bán kính có TtsScript
    public Place? FindNearestPOI(double userLat, double userLon, List<Place> places)
    {
        Place? nearest = null;
        double minDistance = double.MaxValue;

        foreach (var place in places)
        {
            if (string.IsNullOrWhiteSpace(place.TtsScript)) continue;
            var distance = GetDistance(userLat, userLon, place.Latitude, place.Longitude);
            if (distance <= place.Radius && distance < minDistance)
            {
                if (place.LastPlayedAt == null ||
                    (DateTime.Now - place.LastPlayedAt.Value).TotalMinutes >= place.CooldownMinutes)
                {
                    minDistance = distance;
                    nearest = place;
                }
            }
        }
        return nearest;
    }

    private double ToRad(double deg) => deg * Math.PI / 180;
}