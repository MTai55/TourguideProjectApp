using Supabase;
using TourGuideAPP.Services;
using TourGuideAPP.Views;


namespace TourGuideAPP;

public partial class MainPage : ContentPage
{
    private readonly Client _supabase;
    private readonly LocationService _locationService;
    private readonly GeofenceEngine _geofenceEngine;
    private readonly NarrationService _narrationService;
    private string? _lastSpokenPOIId;
    private readonly POIService _poiService;

    public MainPage(Client supabase, LocationService locationService,
                    GeofenceEngine geofenceEngine, POIService poiService,  NarrationService narrationService)
    {
        InitializeComponent();
        _supabase = supabase;
        _locationService = locationService;
        _geofenceEngine = geofenceEngine;
        _poiService = poiService;
         _narrationService = narrationService;
    }
        private async void OnQRScanClicked(object sender, EventArgs e)
    {
         await Navigation.PushAsync(new QRScanPage(_narrationService));
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
     private async void OnMapClicked(object sender, EventArgs e)
    {
         await Navigation.PushAsync(new Views.MapPage(
            _locationService, _poiService, _geofenceEngine, _narrationService));
    }

     private async void OnStartGpsClicked(object sender, EventArgs e)
    {
        GpsLabel.Text = "📍 Đang khởi động GPS...";

        _locationService.LocationChanged += (location) =>
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                GpsLabel.Text = $"📍 {location.Latitude:F6}, {location.Longitude:F6}";

                var pois = _poiService.GetCachedPOIs();
                var nearest = _geofenceEngine.FindNearestPOI(
                    location.Latitude, location.Longitude, pois);

                if (nearest != null)
                {
                    POILabel.Text = $"🏛️ Gần: {nearest.Name}";

                    // Chỉ đọc khi POI mới (chống spam)
                    if (_lastSpokenPOIId != nearest.Id)
                    {
                        _lastSpokenPOIId = nearest.Id;
                        nearest.LastPlayedAt = DateTime.Now;
                        await _narrationService.SpeakAsync(nearest.TtsScript);
                    }
                }
                else
                {
                    POILabel.Text = "🏛️ Không có POI gần đây";
                    _lastSpokenPOIId = null;
                }
            });
        };

        await _locationService.StartAsync();
    }
}