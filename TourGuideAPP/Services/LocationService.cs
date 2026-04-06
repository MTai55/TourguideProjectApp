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
        // Xin quyền GPS
        var status = await Permissions.RequestAsync<Permissions.LocationAlways>();
        if (status != PermissionStatus.Granted)
        {
            // Thử quyền foreground thôi nếu background bị từ chối
            status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                Console.WriteLine("❌ Quyền GPS bị từ chối");
                return;
            }
        }

#if ANDROID
        StartForegroundService();
#else
        StartPollingLoop();
#endif
    }

    public void Stop()
    {
#if ANDROID
        StopForegroundService();
#else
        _cts?.Cancel();
#endif
    }

#if ANDROID
    private void StartForegroundService()
    {
        // Đăng ký nhận vị trí từ foreground service
        TourGuideAPP.Platforms.Android.LocationForegroundService.LocationUpdated -= OnBackgroundLocationUpdated;
        TourGuideAPP.Platforms.Android.LocationForegroundService.LocationUpdated += OnBackgroundLocationUpdated;

        var context = global::Android.App.Application.Context;
        var intent = new global::Android.Content.Intent(
            context,
            typeof(TourGuideAPP.Platforms.Android.LocationForegroundService));

        if (OperatingSystem.IsAndroidVersionAtLeast(26))
            context.StartForegroundService(intent);
        else
            context.StartService(intent);

        Console.WriteLine("✅ Background GPS foreground service đã khởi động");
    }

    private void StopForegroundService()
    {
        TourGuideAPP.Platforms.Android.LocationForegroundService.LocationUpdated -= OnBackgroundLocationUpdated;

        var context = global::Android.App.Application.Context;
        var intent = new global::Android.Content.Intent(
            context,
            typeof(TourGuideAPP.Platforms.Android.LocationForegroundService));
        context.StopService(intent);
    }

    private void OnBackgroundLocationUpdated(double lat, double lon, double? accuracy)
    {
        var location = new Location(lat, lon) { Accuracy = (float?)accuracy };
        LastKnownLocation = location;
        LocationChanged?.Invoke(location);
    }
#else
    private void StartPollingLoop()
    {
        _cts = new CancellationTokenSource();
        _ = Task.Run(async () =>
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    var request = new GeolocationRequest(
                        GeolocationAccuracy.Best,
                        TimeSpan.FromSeconds(5));
                    var location = await Geolocation.GetLocationAsync(request, _cts.Token);
                    if (location != null)
                    {
                        LastKnownLocation = location;
                        LocationChanged?.Invoke(location);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"GPS Error: {ex.Message}");
                }
                await Task.Delay(3000, _cts.Token);
            }
        }, _cts.Token);
    }
#endif

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
