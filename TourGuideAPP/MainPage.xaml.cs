using Supabase;
using TourGuideAPP.Services;
using TourGuideAPP.Views;
using TourGuideAPP.Data.Models;

namespace TourGuideAPP;

public partial class MainPage : ContentPage
{
    private readonly Supabase.Client _supabase;
    private readonly LocationService _locationService;
    private readonly GeofenceEngine _geofenceEngine;
    private readonly POIService _poiService;
    private readonly NarrationService _narrationService;
    private readonly AuthService _authService;
    private readonly PlaceService _placeService;
    private readonly UserProfileService _profileService;
    private string? _lastSpokenPOIId;
    private DateTime _lastReverseGeocodeAt = DateTime.MinValue;
    private string? _lastResolvedLocationText;

    public MainPage(Supabase.Client supabase, LocationService locationService,
                    GeofenceEngine geofenceEngine, POIService poiService,
                    NarrationService narrationService, AuthService authService, PlaceService placeService,
                    UserProfileService profileService)
    {
        InitializeComponent();
        _supabase = supabase;
        _locationService = locationService;
        _geofenceEngine = geofenceEngine;
        _poiService = poiService;
        _narrationService = narrationService;
        _authService = authService;
        _placeService = placeService;
        _profileService = profileService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await TestConnection();
        await LoadPOIs();
        await LoadPlaces();
        UpdateAuthUI();
    }

   private void UpdateAuthUI()
{
    GpsCard.IsVisible = _authService.IsLoggedIn;
    UserLabel.Text = _authService.IsLoggedIn ? "👤 Đã đăng nhập" : "👤 Khách";
}

    private async Task TestConnection()
    {
        try
        {
            await _supabase.InitializeAsync();
            StatusLabel.IsVisible = false;
            StatusLabel.Text = string.Empty;
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"❌ Không kết nối được dữ liệu. {ex.Message}";
            StatusLabel.IsVisible = true;
        }
    }
       private async Task LoadPlaces()
{
    var places = await _placeService.GetAllPlacesAsync();
    
    // Đảm bảo cập nhật UI trên main thread
    MainThread.BeginInvokeOnMainThread(() =>
    {
        PlacesCollection.ItemsSource = null; // Reset trước
        PlacesCollection.ItemsSource = places;
        Console.WriteLine($"✅ Set ItemsSource: {places.Count} items");
    });
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
             await DisplayAlert("Thông báo", "Vui lòng đăng nhập để sử dụng GPS.", "OK");
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
                    UserLocationLabel.Text = nearest.Name;
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
                    UserLocationLabel.Text = await ResolveLocationNameAsync(location);
                    POILabel.Text = "🏛️ Chưa xác định điểm gần nhất";
                    _lastSpokenPOIId = null;
                }
            });
        };
        await _locationService.StartAsync();
    }

    private async Task<string> ResolveLocationNameAsync(Location location)
    {
        // Avoid calling reverse-geocoding too often while GPS updates continuously.
        if (_lastResolvedLocationText is not null &&
            DateTime.UtcNow - _lastReverseGeocodeAt < TimeSpan.FromSeconds(20))
        {
            return _lastResolvedLocationText;
        }

        try
        {
            var placemarks = await Geocoding.Default.GetPlacemarksAsync(location.Latitude, location.Longitude);
            var place = placemarks?.FirstOrDefault();

            if (place is not null)
            {
                var segments = new List<string>();
                if (!string.IsNullOrWhiteSpace(place.Thoroughfare))
                    segments.Add(place.Thoroughfare!);
                if (!string.IsNullOrWhiteSpace(place.SubAdminArea))
                    segments.Add(place.SubAdminArea!);
                if (!string.IsNullOrWhiteSpace(place.AdminArea))
                    segments.Add(place.AdminArea!);

                var text = segments.Count > 0
                    ? string.Join(", ", segments)
                    : $"{location.Latitude:F5}, {location.Longitude:F5}";

                _lastResolvedLocationText = text;
                _lastReverseGeocodeAt = DateTime.UtcNow;
                return text;
            }
        }
        catch
        {
            // Fallback below when geocoding service is unavailable.
        }

        var fallback = $"{location.Latitude:F5}, {location.Longitude:F5}";
        _lastResolvedLocationText = fallback;
        _lastReverseGeocodeAt = DateTime.UtcNow;
        return fallback;
    }

            private async void OnMapClicked(object sender, EventArgs e)
            {
                if (Shell.Current is AppShell appShell)
                {
                    appShell.ActivateMapTab();
                }
                else
                {
                    await Shell.Current.GoToAsync("//MainTabs/MapPage");
                }
            }
            // Khi user bấm vào 1 địa điểm trong danh sách
       // Xử lý khi user bấm vào card địa điểm
// BindingContext của Frame chính là Place object trong danh sách
            private async void OnPlaceTapped(object sender, EventArgs e)
            {
                if (sender is not BindableObject bindable || bindable.BindingContext is not Place place)
                    return;

                await Navigation.PushAsync(new PlaceDetailPage(
                    place,
                    _authService,
                    _locationService,
                    _poiService,
                    _geofenceEngine,
                    _narrationService,
                    _profileService));
            }

    private async void OnQRScanClicked(object sender, EventArgs e)
    {
        if (!_authService.IsLoggedIn)
        {
           await DisplayAlert("Thông báo", "Vui lòng đăng nhập để quét QR.", "OK");
            return;
        }
        await Navigation.PushAsync(new QRScanPage(_narrationService, _profileService, _placeService));
    }

   
 

private async void OnNarrationClicked(object sender, EventArgs e)
{
    if (!_authService.IsLoggedIn)
    {
        await DisplayAlert("Thông báo", "Vui lòng đăng nhập để nghe thuyết minh.", "OK");
        return;
    }
    await _narrationService.SpeakAsync("Chào mừng bạn đến với khu vực Khánh Hội!");
}

}