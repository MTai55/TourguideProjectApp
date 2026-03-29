using Mapsui;
using Mapsui.Projections;
using Mapsui.Tiling;
using Mapsui.Layers;
using Mapsui.UI.Maui;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Mapsui.Nts;
using NetTopologySuite.Geometries;
using TourGuideAPP.Data.Models;
using TourGuideAPP.Services;

namespace TourGuideAPP.Views;

public partial class MapPage : ContentPage
{
    private readonly LocationService _locationService;
    private readonly PlaceService _placeService;
    private readonly GeofenceEngine _geofenceEngine;
    private readonly NarrationService _narrationService;
    private readonly UserProfileService _profileService;
    private readonly AuthService _authService;
    private string? _lastSpokenPlaceId;
    private bool _mapInfoHooked;
    private (double lat, double lon, string? name)? _destination;
    private readonly HttpClient _http = new();

    // Dùng để truyền điểm đến từ PlaceDetailPage trước khi chuyển tab
    public static (double Lat, double Lon, string? Name)? PendingRoute { get; set; }

    public MapPage(LocationService locationService, PlaceService placeService,
                   GeofenceEngine geofenceEngine, NarrationService narrationService,
                   UserProfileService profileService, AuthService authService)
    {
        InitializeComponent();
        _locationService = locationService;
        _placeService = placeService;
        _geofenceEngine = geofenceEngine;
        _narrationService = narrationService;
        _profileService = profileService;
        _authService = authService;
    }

    public MapPage(LocationService locationService, PlaceService placeService,
                   GeofenceEngine geofenceEngine, NarrationService narrationService,
                   UserProfileService profileService, AuthService authService,
                   double destinationLat, double destinationLon, string? destinationName = null)
        : this(locationService, placeService, geofenceEngine, narrationService, profileService, authService)
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
            var baseLayer = OpenStreetMap.CreateTileLayer();
            baseLayer.Name = "BaseMap";
            MyMap.Map.Layers.Add(baseLayer);
        }

        var (x, y) = SphericalMercator.FromLonLat(106.6820, 10.7600);
        MyMap.Map.Navigator.CenterOnAndZoomTo(new MPoint(x, y), MyMap.Map.Navigator.Resolutions[14]);

        if (!_mapInfoHooked)
        {
            MyMap.Info += OnMapInfo;
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

        var oldLayer = MyMap.Map.Layers.FirstOrDefault(l => l.Name == "POIs");
        if (oldLayer is not null)
            MyMap.Map.Layers.Remove(oldLayer);

        var layer = new MemoryLayer
        {
            Name = "POIs",
            Features = features,
            Style = new Mapsui.Styles.SymbolStyle
            {
                SymbolScale = 0.8,
                Fill = new Mapsui.Styles.Brush(Mapsui.Styles.Color.FromArgb(255, 88, 64, 212))
            }
        };

        MyMap.Map.Layers.Add(layer);
    }

    private async void OnMapInfo(object? sender, MapInfoEventArgs e)
    {
        var hitLayers = MyMap.Map.Layers.Where(l => l.Name == "POIs");
        var mapInfo = e.GetMapInfo?.Invoke(hitLayers);
        var hit = mapInfo?.Feature;
        if (hit is null)
            return;

        var id = hit["id"]?.ToString();
        var name = hit["name"]?.ToString();
        var tts = hit["tts"]?.ToString();
        if (string.IsNullOrWhiteSpace(name))
            return;

        var action = await DisplayActionSheetAsync(
            $"📍 {name}",
            "Đóng",
            null,
            "🎙️ Thuyết minh");

        if (action == "🎙️ Thuyết minh")
        {
            var script = string.IsNullOrWhiteSpace(tts)
                ? $"Đây là địa điểm {name}."
                : tts!;
            await _narrationService.SpeakAsync(script);

            if (!string.IsNullOrWhiteSpace(id))
                _lastSpokenPlaceId = id;
        }
    }

    private void StartGPS()
    {
        _locationService.LocationChanged += (location) =>
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                var (x, y) = SphericalMercator.FromLonLat(location.Longitude, location.Latitude);
                MyMap.Map.Navigator.CenterOnAndZoomTo(new MPoint(x, y), MyMap.Map.Navigator.Resolutions[16]);

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
                        await _narrationService.SpeakAsync(nearest.TtsScript ?? nearest.Name);
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
        MyMap.Map.Navigator.CenterOnAndZoomTo(new MPoint(x, y), MyMap.Map.Navigator.Resolutions[16], duration: 400);
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