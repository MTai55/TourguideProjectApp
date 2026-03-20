using Mapsui;
using Mapsui.Projections;
using Mapsui.Tiling;
using Mapsui.Layers;
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
    private readonly POIService _poiService;
    private readonly GeofenceEngine _geofenceEngine;
    private readonly NarrationService _narrationService;
    private readonly UserProfileService _profileService;
    private readonly AuthService _authService;
    private string? _lastSpokenPOIId;
    private POI? _nearestPoi;
    private (double lat, double lon, string? name)? _destination;
    private readonly HttpClient _http = new();

    public MapPage(LocationService locationService, POIService poiService,
                   GeofenceEngine geofenceEngine, NarrationService narrationService,
                   UserProfileService profileService, AuthService authService)
    {
        InitializeComponent();
        _locationService = locationService;
        _poiService = poiService;
        _geofenceEngine = geofenceEngine;
        _narrationService = narrationService;
        _profileService = profileService;
        _authService = authService;
    }

    public MapPage(LocationService locationService, POIService poiService,
                   GeofenceEngine geofenceEngine, NarrationService narrationService,
                   UserProfileService profileService, AuthService authService,
                   double destinationLat, double destinationLon, string? destinationName = null)
        : this(locationService, poiService, geofenceEngine, narrationService, profileService, authService)
    {
        _destination = (destinationLat, destinationLon, destinationName);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        SetupMap();
        await LoadPOIsAsync();
        StartGPS();

        if (_destination is not null)
        {
            if (_locationService.LastKnownLocation is null)
                await _locationService.StartAsync();
            await ShowRouteToDestinationAsync(_destination.Value);
        }
    }

    private void SetupMap()
    {
        MyMap.Map.Layers.Add(OpenStreetMap.CreateTileLayer());

        var (x, y) = SphericalMercator.FromLonLat(106.6820, 10.7600);
        MyMap.Map.Navigator.CenterOnAndZoomTo(new MPoint(x, y), MyMap.Map.Navigator.Resolutions[14]);
    }

    private async Task LoadPOIsAsync()
    {
        var pois = await _poiService.GetAllPOIsAsync();

        var features = new List<IFeature>();
        foreach (var poi in pois)
        {
            var (x, y) = SphericalMercator.FromLonLat(poi.Longitude, poi.Latitude);
            var feature = new PointFeature(new MPoint(x, y));
            feature["name"] = poi.Name;
            features.Add(feature);
        }

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

    private void StartGPS()
    {
        _locationService.LocationChanged += (location) =>
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                var (x, y) = SphericalMercator.FromLonLat(location.Longitude, location.Latitude);
                MyMap.Map.Navigator.CenterOnAndZoomTo(new MPoint(x, y), MyMap.Map.Navigator.Resolutions[16]);

                var pois = _poiService.GetCachedPOIs();
                var nearest = _geofenceEngine.FindNearestPOI(
                    location.Latitude, location.Longitude, pois);

                if (nearest != null)
                {
                    _nearestPoi = nearest;
                    NearestPOILabel.Text = $"🏛️ Gần: {nearest.Name} ({GetDistanceMeters(location.Latitude, location.Longitude, nearest.Latitude, nearest.Longitude):F0}m)";
                    if (_lastSpokenPOIId != nearest.Id)
                    {
                        _lastSpokenPOIId = nearest.Id;
                        nearest.LastPlayedAt = DateTime.Now;
                        await _narrationService.SpeakAsync(nearest.TtsScript);
                    }
                }
                else
                {
                    _nearestPoi = null;
                    NearestPOILabel.Text = "🏛️ Chưa xác định điểm gần nhất";
                    _lastSpokenPOIId = null;
                }
            });
        };
    }

    private async void OnNearestCheckInClicked(object sender, EventArgs e)
    {
        if (!_authService.IsLoggedIn)
        {
            await Shell.Current.GoToAsync("//LoginPage");
            return;
        }

        if (_nearestPoi is null)
        {
            await DisplayAlert("Không có điểm", "Không xác định được địa điểm gần nhất để check-in.", "OK");
            return;
        }

        var current = _locationService.LastKnownLocation;
        if (current is null)
        {
            await DisplayAlert("Lỗi GPS", "Chưa có vị trí GPS. Vui lòng bật GPS.", "OK");
            return;
        }

        var distance = GetDistanceMeters(current.Latitude, current.Longitude, _nearestPoi.Latitude, _nearestPoi.Longitude);
        if (distance <= 100)
        {
            await _profileService.AddHistoryByGpsAsync(_nearestPoi);
            await DisplayAlert("Check-in thành công", $"Đã ghi lịch sử đến {_nearestPoi.Name} (GPS).", "OK");
        }
        else
        {
            await DisplayAlert("Chưa đến gần", $"Khoảng cách còn {distance:F0}m. Hãy đến gần hơn để check-in.", "OK");
        }
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