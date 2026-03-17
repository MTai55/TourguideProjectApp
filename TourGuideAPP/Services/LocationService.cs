namespace TourGuideAPP.Services;

public class LocationService
{
    private CancellationTokenSource? _cts;
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
}