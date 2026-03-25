namespace TourGuideAPP.Services;

public class LocationService
{
    private static readonly TimeSpan MinReverseGeocodeInterval = TimeSpan.FromSeconds(15);
    private const double MinDistanceForRegeocodeMeters = 30;

    private CancellationTokenSource? _cts;
    private Location? _lastGeocodedLocation;
    private DateTime _lastGeocodedAt = DateTime.MinValue;
    private string? _lastResolvedAddress;

    public event Action<Location>? LocationChanged;
    public Location? LastKnownLocation { get; private set; }

    public async Task StartAsync()
    {
        _cts = new CancellationTokenSource();

        // Xin quyền GPS
        var status = await Permissions.RequestAsync<Permissions.LocationAlways>();
        if (status != PermissionStatus.Granted)
        {
            Console.WriteLine("❌ Quyền GPS bị từ chối");
            return;
        }

        // Bắt đầu lấy vị trí liên tục
        _ = Task.Run(async () =>
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    var request = new GeolocationRequest(
                        GeolocationAccuracy.Best,
                        TimeSpan.FromSeconds(5)
                    );
                    var location = await Geolocation.GetLocationAsync(request, _cts.Token);
                    if (location != null)
                    {
                        LastKnownLocation = location;
                        LocationChanged?.Invoke(location);
                        Console.WriteLine($"📍 Vị trí: {location.Latitude}, {location.Longitude}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"GPS Error: {ex.Message}");
                }
                await Task.Delay(3000, _cts.Token); // Cập nhật mỗi 3 giây
            }
        }, _cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
    }

    public async Task<string> GetAddressAsync(Location location, CancellationToken cancellationToken = default)
    {
        if (_lastGeocodedLocation is not null &&
            DateTime.UtcNow - _lastGeocodedAt < MinReverseGeocodeInterval)
        {
            var distance = Location.CalculateDistance(
                _lastGeocodedLocation,
                location,
                DistanceUnits.Kilometers) * 1000;

            if (distance < MinDistanceForRegeocodeMeters && !string.IsNullOrWhiteSpace(_lastResolvedAddress))
                return _lastResolvedAddress;
        }

        try
        {
            var placemarks = await Geocoding.Default.GetPlacemarksAsync(location.Latitude, location.Longitude);
            var placemark = placemarks?.FirstOrDefault();

            var resolved = placemark is null
                ? $"{location.Latitude:F6}, {location.Longitude:F6}"
                : FormatPlacemark(placemark);

            _lastGeocodedLocation = location;
            _lastGeocodedAt = DateTime.UtcNow;
            _lastResolvedAddress = resolved;

            return resolved;
        }
        catch
        {
            return _lastResolvedAddress ?? $"{location.Latitude:F6}, {location.Longitude:F6}";
        }
    }

    private static string FormatPlacemark(Placemark placemark)
    {
        var parts = new[]
        {
            placemark.FeatureName,
            placemark.SubThoroughfare,
            placemark.Thoroughfare,
            placemark.SubLocality,
            placemark.Locality,
            placemark.SubAdminArea,
            placemark.AdminArea
        };

        var normalized = parts
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p!.Trim())
            .Distinct()
            .ToList();

        return normalized.Count == 0
            ? "Không lấy được tên địa chỉ"
            : string.Join(", ", normalized);
    }
}