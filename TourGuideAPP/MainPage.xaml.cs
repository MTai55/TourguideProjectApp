using Supabase;
using TourGuideAPP.Services;
using TourGuideAPP.Views;

namespace TourGuideAPP;

public partial class MainPage : ContentPage
{
    private readonly Supabase.Client _supabase;
    private readonly LocationService _locationService;
    private readonly GeofenceEngine _geofenceEngine;
    private readonly POIService _poiService;
    private readonly NarrationService _narrationService;
    private readonly AuthService _authService;
    private string? _lastSpokenPOIId;

    public MainPage(Supabase.Client supabase, LocationService locationService,
                    GeofenceEngine geofenceEngine, POIService poiService,
                    NarrationService narrationService, AuthService authService)
    {
        InitializeComponent();
        _supabase = supabase;
        _locationService = locationService;
        _geofenceEngine = geofenceEngine;
        _poiService = poiService;
        _narrationService = narrationService;
        _authService = authService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await TestConnection();
        await LoadPOIs();
        UpdateAuthUI();
    }

   private void UpdateAuthUI()
{
    LoginButton.IsVisible = !_authService.IsLoggedIn;
    LogoutButton.IsVisible = _authService.IsLoggedIn;
    GpsCard.IsVisible = _authService.IsLoggedIn;
    UserLabel.Text = _authService.IsLoggedIn ? "👤 Đã đăng nhập" : "👤 Khách";
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
        if (!_authService.IsLoggedIn)
        {
             await Navigation.PushAsync(new LoginPage(_authService));
            return;
        }

        GpsLabel.Text = "📍 Đang khởi động GPS...";
        _locationService.LocationChanged += (location) =>
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                GpsLabel.Text = $"📍 {location.Latitude:F6}, {location.Longitude:F6}";
                var pois = _poiService.GetCachedPOIs();
                var nearest = _geofenceEngine.FindNearestPOI(location.Latitude, location.Longitude, pois);
                if (nearest != null)
                {
                    POILabel.Text = $"🏛️ Gần: {nearest.Name}";
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

    private async void OnMapClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new MapPage(
            _locationService, _poiService, _geofenceEngine, _narrationService));
    }

    private async void OnQRScanClicked(object sender, EventArgs e)
    {
        if (!_authService.IsLoggedIn)
        {
           await Shell.Current.GoToAsync("//LoginPage");
            return;
        }
        await Navigation.PushAsync(new QRScanPage(_narrationService));
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        await _authService.LogoutAsync();
        await Shell.Current.GoToAsync("//LoginPage");
    }
    private async void OnLoginClicked(object sender, EventArgs e)
{
    await Navigation.PushAsync(new LoginPage(_authService));
}

private async void OnNarrationClicked(object sender, EventArgs e)
{
    if (!_authService.IsLoggedIn)
    {
        await Navigation.PushAsync(new LoginPage(_authService));
        return;
    }
    await _narrationService.SpeakAsync("Chào mừng bạn đến với khu vực Khánh Hội!");
}

private void OnPlaceSelected(object sender, SelectionChangedEventArgs e)
{
    // Sau này navigate đến PlaceDetailPage
}
}