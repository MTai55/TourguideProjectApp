using Mapsui;
using Mapsui.Projections;
using Mapsui.Tiling;
using Mapsui.Tiling.Layers;
using BruTile.Web;
using BruTile.Predefined;
using Mapsui.Layers;
using Mapsui.UI.Maui;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Mapsui.Nts;
using NetTopologySuite.Geometries;
using TourGuideAPP.Data.Models;
using TourGuideAPP.Services;
using Microsoft.Maui.ApplicationModel.Communication;

namespace TourGuideAPP.Views;

public partial class MapPage : ContentPage
{
    private readonly LocationService _locationService;
    private readonly PlaceService _placeService;
    private readonly GeofenceEngine _geofenceEngine;
    private readonly NarrationService _narrationService;
    private readonly AuthService _authService;
    private string? _lastSpokenPlaceId;
    private bool _mapInfoHooked;
    private (double lat, double lon, string? name)? _destination;
    private readonly HttpClient _http = new();
    private bool _followUserLocation = true;   // tự follow GPS
    private bool _programmaticNav = false;     // đang nav bằng code (không phải user kéo)
    private Place? _selectedPlace;

    // Dùng để truyền điểm đến từ PlaceDetailPage trước khi chuyển tab
    public static (double Lat, double Lon, string? Name)? PendingRoute { get; set; }

    public MapPage(LocationService locationService, PlaceService placeService,
                   GeofenceEngine geofenceEngine, NarrationService narrationService,
                   AuthService authService)
    {
        InitializeComponent();
        _locationService = locationService;
        _placeService = placeService;
        _geofenceEngine = geofenceEngine;
        _narrationService = narrationService;
        _authService = authService;
    }

    public MapPage(LocationService locationService, PlaceService placeService,
                   GeofenceEngine geofenceEngine, NarrationService narrationService,
                   AuthService authService,
                   double destinationLat, double destinationLon, string? destinationName = null)
        : this(locationService, placeService, geofenceEngine, narrationService, authService)
    {
        _destination = (destinationLat, destinationLon, destinationName);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        SetupMap();
        await LoadPOIsAsync();
        StartGPS();

        // Nhận điểm đến từ PlaceDetailPage nếu có
        if (PendingRoute is { } pending)
        {
            PendingRoute = null;
            _destination = (pending.Lat, pending.Lon, pending.Name);
        }

        if (_destination is not null)
        {
            var dest = _destination.Value;
            _destination = null;
            if (_locationService.LastKnownLocation is null)
                await _locationService.StartAsync();
            await ShowRouteToDestinationAsync(dest);
        }
    }

    private void SetupMap()
    {
        MyMap.Map ??= new Mapsui.Map();

        if (!MyMap.Map.Layers.Any(l => l.Name == "BaseMap"))
        {
            var tileSource = new HttpTileSource(
                new GlobalSphericalMercator(),
                "https://{s}.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}.png",
                new[] { "a", "b", "c" },
                name: "CartoDB Voyager");
            var baseLayer = new TileLayer(tileSource) { Name = "BaseMap" };
            MyMap.Map.Layers.Add(baseLayer);
        }

        var (x, y) = SphericalMercator.FromLonLat(106.6820, 10.7600);
        MyMap.Map.Navigator.CenterOnAndZoomTo(new MPoint(x, y), MyMap.Map.Navigator.Resolutions[14]);

        if (!_mapInfoHooked)
        {
            MyMap.Info += OnMapInfo;
            MyMap.Map.Navigator.ViewportChanged += (s, e) =>
            {
                // Nếu viewport thay đổi không phải do code → user đang kéo map
                if (!_programmaticNav)
                    _followUserLocation = false;
            };
            _mapInfoHooked = true;
        }
    }

    private async Task LoadPOIsAsync()
    {
        var places = await _placeService.GetAllPlacesAsync();

        var features = new List<IFeature>();
        foreach (var place in places)
        {
            var (x, y) = SphericalMercator.FromLonLat(place.Longitude, place.Latitude);
            var feature = new PointFeature(new MPoint(x, y));
            feature["id"] = place.PlaceId.ToString();
            feature["name"] = place.Name;
            feature["tts"] = place.TtsScript;
            features.Add(feature);
        }

        foreach (var name in new[] { "POIsGlow", "POIs" })
        {
            var old = MyMap.Map.Layers.FirstOrDefault(l => l.Name == name);
            if (old is not null) MyMap.Map.Layers.Remove(old);
        }

        // Lớp ngoài — vòng glow mờ đỏ
        MyMap.Map.Layers.Add(new MemoryLayer
        {
            Name = "POIsGlow",
            Features = features,
            Style = new Mapsui.Styles.SymbolStyle
            {
                SymbolScale = 1.6,
                Fill = new Mapsui.Styles.Brush(Mapsui.Styles.Color.FromArgb(45, 233, 69, 96)),
                Outline = new Mapsui.Styles.Pen(Mapsui.Styles.Color.FromArgb(90, 233, 69, 96), 1.5)
            }
        });

        // Lớp trong — chấm đặc đỏ với viền trắng
        MyMap.Map.Layers.Add(new MemoryLayer
        {
            Name = "POIs",
            Features = features,
            Style = new Mapsui.Styles.SymbolStyle
            {
                SymbolScale = 0.5,
                Fill = new Mapsui.Styles.Brush(Mapsui.Styles.Color.FromArgb(255, 233, 69, 96)),
                Outline = new Mapsui.Styles.Pen(Mapsui.Styles.Color.White, 3)
            }
        });
    }

    private void OnMapInfo(object? sender, MapInfoEventArgs e)
    {
        var hitLayers = MyMap.Map.Layers.Where(l => l.Name == "POIs");
        var mapInfo = e.GetMapInfo?.Invoke(hitLayers);
        var hit = mapInfo?.Feature;
        if (hit is null)
            return;

        var id = hit["id"]?.ToString();
        if (string.IsNullOrWhiteSpace(id))
            return;

        var place = _placeService.GetCachedPlaces()
            .FirstOrDefault(p => p.PlaceId.ToString() == id);
        if (place is null)
            return;

        MainThread.BeginInvokeOnMainThread(() => ShowPlaceCard(place));
    }

    private void ShowPlaceCard(Place place)
    {
        _selectedPlace = place;

        CardName.Text = place.Name;

        // Status badge
        bool isOpen = IsPlaceOpen(place);
        CardStatusLabel.Text = isOpen ? "Đang mở" : "Đóng cửa";
        CardStatusLabel.TextColor = isOpen
            ? Color.FromArgb("#4CAF50")
            : Color.FromArgb("#E94560");
        CardStatusBadge.BackgroundColor = isOpen
            ? Color.FromArgb("#1B3A28")
            : Color.FromArgb("#3A1B20");

        // Stars + rating
        if (place.AverageRating.HasValue)
        {
            int full = (int)Math.Round(place.AverageRating.Value);
            CardStars.Text = new string('★', full) + new string('☆', 5 - full);
            CardRating.Text = place.AverageRating.Value.ToString("F1");
            CardReviews.Text = place.TotalReviews.HasValue ? $"({place.TotalReviews})" : "";
        }
        else
        {
            CardStars.Text = "☆☆☆☆☆";
            CardRating.Text = "";
            CardReviews.Text = "Chưa có đánh giá";
        }

        // Tag chips from Specialty
        CardTags.Children.Clear();
        if (!string.IsNullOrWhiteSpace(place.Specialty))
        {
            foreach (var tag in place.Specialty.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                var chip = new Border
                {
                    BackgroundColor = Color.FromArgb("#26201A"),
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(6) },
                    Stroke = Colors.Transparent,
                    Padding = new Thickness(10, 4)
                };
                chip.Content = new Label
                {
                    Text = tag.Trim(),
                    FontSize = 11,
                    TextColor = Color.FromArgb("#C8A96E")
                };
                CardTags.Children.Add(chip);
            }
        }

        // Address
        CardAddress.Text = string.IsNullOrWhiteSpace(place.Address) ? "—" : place.Address;

        // Hours
        CardHours.Text = place.OpenTimeDisplay;

        PlaceCard.IsVisible = true;
    }

    private static bool IsPlaceOpen(Place place)
    {
        if (place.OpenTime is null || place.CloseTime is null)
            return false;
        if (!TimeSpan.TryParse(place.OpenTime, out var open) ||
            !TimeSpan.TryParse(place.CloseTime, out var close))
            return false;
        var now = DateTime.Now.TimeOfDay;
        return now >= open && now <= close;
    }

    // Màu bình thường của từng nút
    private static readonly Color _primaryNormal  = Color.FromArgb("#C8A96E");
    private static readonly Color _primaryHover   = Color.FromArgb("#E8C98E");
    private static readonly Color _secondaryNormal = Color.FromArgb("#26201A");
    private static readonly Color _secondaryHover  = Color.FromArgb("#3E3028");

    private void OnCardBtnHoverEnter(object sender, PointerEventArgs e)
    {
        if (sender is not Border btn) return;
        btn.BackgroundColor = ReferenceEquals(btn, BtnDirections) ? _primaryHover : _secondaryHover;
    }

    private void OnCardBtnHoverExit(object sender, PointerEventArgs e)
    {
        if (sender is not Border btn) return;
        btn.BackgroundColor = ReferenceEquals(btn, BtnDirections) ? _primaryNormal : _secondaryNormal;
    }

    private static async Task FlashButton(Border btn, bool isPrimary)
    {
        var bright = isPrimary ? Color.FromArgb("#FFDF9F") : Color.FromArgb("#4E3E30");
        var normal = isPrimary ? Color.FromArgb("#C8A96E") : Color.FromArgb("#26201A");
        btn.BackgroundColor = bright;
        await Task.Delay(120);
        btn.BackgroundColor = normal;
    }

    private void OnPlaceCardDismiss(object sender, TappedEventArgs e)
    {
        PlaceCard.IsVisible = false;
        _selectedPlace = null;
    }

    private async void OnCardDirections(object sender, TappedEventArgs e)
    {
        if (_selectedPlace is null) return;
        await FlashButton(BtnDirections, isPrimary: true);
        PlaceCard.IsVisible = false;
        var dest = (_selectedPlace.Latitude, _selectedPlace.Longitude, (string?)_selectedPlace.Name);
        _selectedPlace = null;
        if (_locationService.LastKnownLocation is null)
            await _locationService.StartAsync();
        await ShowRouteToDestinationAsync(dest);
    }

    private async void OnCardNarrate(object sender, TappedEventArgs e)
    {
        if (_selectedPlace is null) return;
        await FlashButton(BtnNarrate, isPrimary: false);
        _lastSpokenPlaceId = _selectedPlace.PlaceId.ToString();
        await _narrationService.SpeakAsync(_selectedPlace.GetScriptForLocale(_narrationService.PreferredLocale));
    }

    private async void OnCardCall(object sender, TappedEventArgs e)
    {
        if (_selectedPlace is null) return;
        await FlashButton(BtnCall, isPrimary: false);
        if (string.IsNullOrWhiteSpace(_selectedPlace.Phone))
        {
            DisplayAlert("Thông báo", "Địa điểm này chưa có số điện thoại.", "OK");
            return;
        }
        try { PhoneDialer.Open(_selectedPlace.Phone); }
        catch { DisplayAlert("Lỗi", "Không thể mở ứng dụng gọi điện.", "OK"); }
    }

    private async void OnCardDetail(object sender, TappedEventArgs e)
    {
        if (_selectedPlace is null) return;
        await FlashButton(BtnDetail, isPrimary: false);
        var place = _selectedPlace;
        PlaceCard.IsVisible = false;
        _selectedPlace = null;
        var detailPage = new PlaceDetailPage(place, _authService, _locationService,
                                             _geofenceEngine, _narrationService);
        await Navigation.PushAsync(detailPage);
    }

    private void UpdateUserMarker(double lat, double lon)
    {
        // Xóa layer cũ
        foreach (var name in new[] { "UserLocationGlow", "UserLocationDot" })
        {
            var old = MyMap.Map.Layers.FirstOrDefault(l => l.Name == name);
            if (old is not null) MyMap.Map.Layers.Remove(old);
        }

        var (x, y) = SphericalMercator.FromLonLat(lon, lat);
        var point = new MPoint(x, y);

        // Lớp ngoài — vòng glow mờ xanh
        MyMap.Map.Layers.Add(new MemoryLayer
        {
            Name = "UserLocationGlow",
            Features = new[] { new PointFeature(point) },
            Style = new Mapsui.Styles.SymbolStyle
            {
                SymbolScale = 1.6,
                Fill = new Mapsui.Styles.Brush(Mapsui.Styles.Color.FromArgb(45, 0, 140, 255)),
                Outline = new Mapsui.Styles.Pen(Mapsui.Styles.Color.FromArgb(90, 0, 140, 255), 1.5)
            }
        });

        // Lớp trong — chấm đặc xanh với viền trắng
        MyMap.Map.Layers.Add(new MemoryLayer
        {
            Name = "UserLocationDot",
            Features = new[] { new PointFeature(point) },
            Style = new Mapsui.Styles.SymbolStyle
            {
                SymbolScale = 0.5,
                Fill = new Mapsui.Styles.Brush(Mapsui.Styles.Color.FromArgb(255, 30, 144, 255)),
                Outline = new Mapsui.Styles.Pen(Mapsui.Styles.Color.White, 3)
            }
        });

        MyMap.Map.RefreshData();
    }

    private void StartGPS()
    {
        _locationService.LocationChanged += (location) =>
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                var (x, y) = SphericalMercator.FromLonLat(location.Longitude, location.Latitude);

                // Chỉ tự center khi đang ở chế độ follow
                if (_followUserLocation)
                {
                    _programmaticNav = true;
                    MyMap.Map.Navigator.CenterOnAndZoomTo(new MPoint(x, y), MyMap.Map.Navigator.Resolutions[16]);
                    _programmaticNav = false;
                }

                UpdateUserMarker(location.Latitude, location.Longitude);

                var address = await _locationService.GetAddressAsync(location);
                CurrentAddressLabel.Text = $"📍 Địa chỉ hiện tại: {address}";

                var places = _placeService.GetCachedPlaces();
                var nearest = _geofenceEngine.FindNearestPOI(
                    location.Latitude, location.Longitude, places);

                // Khi đang chỉ đường, không cho GPS callback ghi đè label
                if (CancelRoutePanel.IsVisible)
                    return;

                if (nearest != null)
                {
                    NearestPOILabel.Text = $"🏛️ Gần: {nearest.Name} ({GetDistanceMeters(location.Latitude, location.Longitude, nearest.Latitude, nearest.Longitude):F0}m)";
                    var nearestId = nearest.PlaceId.ToString();
                    if (_lastSpokenPlaceId != nearestId)
                    {
                        _lastSpokenPlaceId = nearestId;
                        nearest.LastPlayedAt = DateTime.Now;
                        await _narrationService.SpeakAsync(nearest.GetScriptForLocale(_narrationService.PreferredLocale));
                    }
                }
                else
                {
                    NearestPOILabel.Text = "🏛️ Chưa xác định điểm gần nhất";
                    _lastSpokenPlaceId = null;
                }
            });
        };
    }

    private static double GetDistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        static double ToRad(double deg) => deg * Math.PI / 180;

        var R = 6371000;
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private async Task ShowRouteToDestinationAsync((double lat, double lon, string? name) destination)
    {
        var origin = _locationService.LastKnownLocation;
        if (origin is null)
        {
            NearestPOILabel.Text = "📍 Chưa có vị trí hiện tại để chỉ đường";
            return;
        }

        // OSRM public demo server (OK cho demo, không đảm bảo SLA).
        var url =
            $"https://router.project-osrm.org/route/v1/driving/{origin.Longitude},{origin.Latitude};{destination.lon},{destination.lat}?overview=full&geometries=geojson";

        OsrmRouteResponse? res;
        try
        {
            res = await _http.GetFromJsonAsync<OsrmRouteResponse>(url);
        }
        catch (Exception ex)
        {
            NearestPOILabel.Text = $"❌ Lỗi lấy đường đi: {ex.Message}";
            return;
        }

        var coords = res?.Routes?.FirstOrDefault()?.Geometry?.Coordinates;
        if (coords is null || coords.Count < 2)
        {
            NearestPOILabel.Text = "❌ Không lấy được đường đi";
            return;
        }

        // Remove old route layer if exists
        var old = MyMap.Map.Layers.FirstOrDefault(l => l.Name == "Route");
        if (old is not null) MyMap.Map.Layers.Remove(old);

        var routePoints = coords
            .Select(c =>
            {
                var (x, y) = SphericalMercator.FromLonLat(c[0], c[1]);
                return new MPoint(x, y);
            })
            .ToList();

        var routeCoords = routePoints.Select(p => new Coordinate(p.X, p.Y)).ToArray();
        var routeFeature = new GeometryFeature(new LineString(routeCoords));

        var routeLayer = new MemoryLayer
        {
            Name = "Route",
            Features = new[] { routeFeature },
            Style = new Mapsui.Styles.VectorStyle
            {
                Line = new Mapsui.Styles.Pen(Mapsui.Styles.Color.FromArgb(255, 233, 69, 96), 5) // #E94560
            }
        };

        // Destination marker
        var (dx, dy) = SphericalMercator.FromLonLat(destination.lon, destination.lat);
        var destFeature = new PointFeature(new MPoint(dx, dy));
        destFeature["name"] = destination.name ?? "Điểm đến";
        var destLayer = new MemoryLayer
        {
            Name = "Destination",
            Features = new[] { destFeature },
            Style = new Mapsui.Styles.SymbolStyle
            {
                SymbolScale = 1.0,
                Fill = new Mapsui.Styles.Brush(Mapsui.Styles.Color.FromArgb(255, 233, 69, 96))
            }
        };

        var oldDest = MyMap.Map.Layers.FirstOrDefault(l => l.Name == "Destination");
        if (oldDest is not null) MyMap.Map.Layers.Remove(oldDest);

        MyMap.Map.Layers.Add(routeLayer);
        MyMap.Map.Layers.Add(destLayer);

        // Zoom to route bounds
        var extent = routeFeature.Extent;
        if (extent is not null)
            MyMap.Map.Navigator.ZoomToBox(extent, MBoxFit.Fit);

        NearestPOILabel.Text = $"🧭 Đang chỉ đường tới: {destination.name ?? "điểm đến"}";
        CancelRoutePanel.IsVisible = true;
    }

    private void OnZoomInClicked(object sender, TappedEventArgs e)
    {
        var nav = MyMap.Map.Navigator;
        nav.ZoomIn(duration: 200);
        MyMap.Map.RefreshData();
    }

    private void OnZoomOutClicked(object sender, TappedEventArgs e)
    {
        var nav = MyMap.Map.Navigator;
        nav.ZoomOut(duration: 200);
        MyMap.Map.RefreshData();
    }

    private void OnTrackLocationClicked(object sender, TappedEventArgs e)
    {
        var loc = _locationService.LastKnownLocation;
        if (loc is null) return;
        var (x, y) = SphericalMercator.FromLonLat(loc.Longitude, loc.Latitude);
        _followUserLocation = true;
        _programmaticNav = true;
        MyMap.Map.Navigator.CenterOnAndZoomTo(new MPoint(x, y), MyMap.Map.Navigator.Resolutions[16]);
        _programmaticNav = false;
        MyMap.Map.RefreshData();
    }

    private void OnCancelRouteClicked(object sender, TappedEventArgs e)
    {
        var routeLayer = MyMap.Map.Layers.FirstOrDefault(l => l.Name == "Route");
        if (routeLayer is not null) MyMap.Map.Layers.Remove(routeLayer);

        var destLayer = MyMap.Map.Layers.FirstOrDefault(l => l.Name == "Destination");
        if (destLayer is not null) MyMap.Map.Layers.Remove(destLayer);

        CancelRoutePanel.IsVisible = false;
        NearestPOILabel.Text = "Chưa xác định điểm gần nhất";
        MyMap.Map.RefreshData();
    }

    private sealed class OsrmRouteResponse
    {
        [JsonPropertyName("routes")]
        public List<OsrmRoute>? Routes { get; set; }
    }

    private sealed class OsrmRoute
    {
        [JsonPropertyName("geometry")]
        public OsrmGeometry? Geometry { get; set; }
    }

    private sealed class OsrmGeometry
    {
        // GeoJSON LineString coordinates: [[lon,lat],[lon,lat],...]
        [JsonPropertyName("coordinates")]
        public List<double[]>? Coordinates { get; set; }
    }
}