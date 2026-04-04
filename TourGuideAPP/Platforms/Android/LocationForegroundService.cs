using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;

namespace TourGuideAPP.Platforms.Android;

[Service(
    Name = "com.tourguide.LocationForegroundService",
    ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeLocation)]
public class LocationForegroundService : Service
{
    public const string ChannelId = "tourguide_location_channel";
    public const int NotificationId = 1001;

    private CancellationTokenSource? _cts;

    // LocationService đăng ký vào đây để nhận cập nhật vị trí
    public static event Action<double, double, double?>? LocationUpdated;

    public override IBinder? OnBind(Intent? intent) => null;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        CreateNotificationChannel();
        var notification = BuildNotification();

        if (OperatingSystem.IsAndroidVersionAtLeast(29))
            StartForeground(NotificationId, notification,
                global::Android.Content.PM.ForegroundService.TypeLocation);
        else
            StartForeground(NotificationId, notification);

        StartTracking();
        return StartCommandResult.Sticky; // Android tự khởi động lại nếu bị kill
    }

    private void StartTracking()
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
                    if (location is not null)
                        LocationUpdated?.Invoke(location.Latitude, location.Longitude, location.Accuracy);
                }
                catch (System.OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    Console.WriteLine($"[BackgroundGPS] Error: {ex.Message}");
                }

                try { await Task.Delay(3000, _cts.Token); }
                catch (System.OperationCanceledException) { break; }
            }
        }, _cts.Token);
    }

    public override void OnDestroy()
    {
        _cts?.Cancel();
        base.OnDestroy();
    }

    private void CreateNotificationChannel()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26)) return;

        var channel = new NotificationChannel(
            ChannelId,
            "Theo dõi vị trí",
            NotificationImportance.Low)
        {
            Description = "TourGuide đang theo dõi vị trí để gợi ý địa điểm gần bạn"
        };
        channel.SetShowBadge(false);

        var manager = (NotificationManager?)GetSystemService(NotificationService);
        manager?.CreateNotificationChannel(channel);
    }

    private Notification BuildNotification()
    {
        var intent = new Intent(this, typeof(MainActivity));
        intent.AddFlags(ActivityFlags.SingleTop);
        var pendingFlags = PendingIntentFlags.UpdateCurrent |
                          (OperatingSystem.IsAndroidVersionAtLeast(23)
                              ? PendingIntentFlags.Immutable
                              : 0);
        var pendingIntent = PendingIntent.GetActivity(this, 0, intent, pendingFlags);

        return new NotificationCompat.Builder(this, ChannelId)
            .SetContentTitle("TourGuide đang hoạt động")
            .SetContentText("Đang theo dõi vị trí để gợi ý địa điểm gần bạn")
            .SetSmallIcon(Resource.Mipmap.appicon)
            .SetContentIntent(pendingIntent)
            .SetOngoing(true)
            .SetPriority(NotificationCompat.PriorityLow)
            .SetCategory(NotificationCompat.CategoryService)
            .Build();
    }
}
