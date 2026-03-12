using Supabase;
using TourGuideAPP.Services;

namespace TourGuideAPP;

public partial class MainPage : ContentPage
{
    private readonly Client _supabase;
    private readonly LocationService _locationService;
    private readonly GeofenceEngine _geofenceEngine;
    private readonly POIService _poiService;

    public MainPage(Client supabase, LocationService locationService,
                    GeofenceEngine geofenceEngine, POIService poiService)
    {
        InitializeComponent();
        _supabase = supabase;
        _locationService = locationService;
        _geofenceEngine = geofenceEngine;
        _poiService = poiService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await TestConnection();
        await LoadPOIs();
    }

    private async Task TestConnection()
    {
        try
        {
            await _supabase.InitializeAsync();
            StatusLabel.Text = "✅ Supabase OK!";
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"❌ {ex.Message}";
        }
    }

    private async Task LoadPOIs()
    {
        var pois = await _poiService.GetAllPOIsAsync();
        POICountLabel.Text = $"Đã tải {pois.Count} POI từ Supabase";
    }

    private async void OnStartGpsClicked(object sender, EventArgs e)
    {
        GpsLabel.Text = "📍 Đang khởi động GPS...";

        _locationService.LocationChanged += (location) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                GpsLabel.Text = $"📍 {location.Latitude:F6}, {location.Longitude:F6}";

                // Kiểm tra Geofence
                var pois = _poiService.GetCachedPOIs();
                var nearest = _geofenceEngine.FindNearestPOI(
                    location.Latitude, location.Longitude, pois);

                if (nearest != null)
                    POILabel.Text = $"🏛️ Gần: {nearest.Name}";
                else
                    POILabel.Text = "🏛️ Không có POI gần đây";
            });
        };

        await _locationService.StartAsync();
    }
}