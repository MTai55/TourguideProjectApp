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
    private readonly NarrationService _narrationService;
    private readonly AuthService _authService;
    private readonly PlaceService _placeService;
    private string? _lastSpokenPlaceId;
    private DateTime _lastReverseGeocodeAt = DateTime.MinValue;
    private string? _lastResolvedLocationText;
    private List<Place> _allPlaces = new();
    private string _selectedCategory = "all";

    private static readonly Dictionary<string, string[]> CategoryKeywords = new()
    {
        ["cafe"]      = new[] { "cà phê", "cafe", "coffee", "caphe" },
        ["rice"]      = new[] { "cơm", "nhà hàng", "quán ăn", "com ", "bữa ăn" },
        ["beer"]      = new[] { "nhậu", "bia", "quán nhậu", "beer" },
        ["bubbletea"] = new[] { "trà sữa", "bubble", "trà" }
    };

    public MainPage(Supabase.Client supabase, LocationService locationService,
                    GeofenceEngine geofenceEngine, NarrationService narrationService,
                    AuthService authService, PlaceService placeService)
    {
        InitializeComponent();
        _supabase = supabase;
        _locationService = locationService;
        _geofenceEngine = geofenceEngine;
        _narrationService = narrationService;
        _authService = authService;
        _placeService = placeService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await TestConnection();
        await LoadPlaces();
        UpdateAuthUI();
    }

   private void UpdateAuthUI()
{
    GpsCard.IsVisible = true;
    UserLabel.Text = "👤 Khách";
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
        _allPlaces = places;
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        var searchText = SearchEntry.Text?.Trim().ToLower() ?? string.Empty;
        var filtered = _allPlaces.AsEnumerable();

        // Lọc theo category
        if (_selectedCategory != "all" && CategoryKeywords.TryGetValue(_selectedCategory, out var keywords))
        {
            filtered = filtered.Where(p =>
                keywords.Any(kw =>
                    (p.Name?.ToLower().Contains(kw) ?? false) ||
                    (p.Description?.ToLower().Contains(kw) ?? false) ||
                    (p.Specialty?.ToLower().Contains(kw) ?? false)));
        }

        // Lọc theo từ khóa tìm kiếm
        if (!string.IsNullOrEmpty(searchText))
        {
            filtered = filtered.Where(p =>
                (p.Name?.ToLower().Contains(searchText) ?? false) ||
                (p.Address?.ToLower().Contains(searchText) ?? false) ||
                (p.Description?.ToLower().Contains(searchText) ?? false) ||
                (p.Specialty?.ToLower().Contains(searchText) ?? false));
        }

        var result = filtered.ToList();
        MainThread.BeginInvokeOnMainThread(() =>
        {
            PlacesCollection.ItemsSource = null;
            PlacesCollection.ItemsSource = result;
        });
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e) => ApplyFilters();

    private void OnCategoryAllTapped(object sender, TappedEventArgs e)      => SelectCategory("all");
    private void OnCategoryCafeTapped(object sender, TappedEventArgs e)     => SelectCategory("cafe");
    private void OnCategoryRiceTapped(object sender, TappedEventArgs e)     => SelectCategory("rice");
    private void OnCategoryBeerTapped(object sender, TappedEventArgs e)     => SelectCategory("beer");
    private void OnCategoryBubbleTeaTapped(object sender, TappedEventArgs e)=> SelectCategory("bubbletea");

    private void SelectCategory(string category)
    {
        _selectedCategory = category;
        SetChipState(ChipAll,       ChipAllLabel,       category == "all");
        SetChipState(ChipCafe,      ChipCafeLabel,      category == "cafe");
        SetChipState(ChipRice,      ChipRiceLabel,      category == "rice");
        SetChipState(ChipBeer,      ChipBeerLabel,      category == "beer");
        SetChipState(ChipBubbleTea, ChipBubbleTeaLabel, category == "bubbletea");
        ApplyFilters();
    }

    private static void SetChipState(Border chip, Label label, bool selected)
    {
        chip.BackgroundColor = Color.FromArgb(selected ? "#1A1410" : "#26201A");
        chip.Stroke          = new SolidColorBrush(selected ? Color.FromArgb("#C8A96E") : Colors.Transparent);
        chip.StrokeThickness = selected ? 1 : 0;
        label.TextColor      = Color.FromArgb(selected ? "#C8A96E" : "#8A7560");
        label.FontAttributes = selected ? FontAttributes.Bold : FontAttributes.None;
    }
    private async void OnStartGpsClicked(object sender, EventArgs e)
    {
        GpsLabel.Text = "📍 Đang khởi động GPS...";
        _locationService.LocationChanged += (location) =>
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                GpsLabel.Text = $"📍 {location.Latitude:F6}, {location.Longitude:F6}";
                var places = _placeService.GetCachedPlaces();
                var nearest = _geofenceEngine.FindNearestPOI(location.Latitude, location.Longitude, places);

                if (nearest != null)
                {
                    UserLocationLabel.Text = nearest.Name;
                    POILabel.Text = $"🏛️ Gần: {nearest.Name}";
                    var nearestId = nearest.PlaceId.ToString();
                    if (_lastSpokenPlaceId != nearestId)
                    {
                        _lastSpokenPlaceId = nearestId;
                        nearest.LastPlayedAt = DateTime.Now;
                        await _narrationService.SpeakAsync(nearest.TtsScript ?? nearest.Name, nearest.TtsLocale);
                    }
                }
                else
                {
                    UserLocationLabel.Text = await ResolveLocationNameAsync(location);
                    POILabel.Text = "🏛️ Chưa xác định điểm gần nhất";
                    _lastSpokenPlaceId = null;
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
                    _geofenceEngine,
                    _narrationService));
            }




private async void OnNarrationClicked(object sender, EventArgs e)
{
    await _narrationService.SpeakAsync("Chào mừng bạn đến với khu vực Khánh Hội!");
}

}