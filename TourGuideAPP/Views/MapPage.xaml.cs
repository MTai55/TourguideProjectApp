using Mapsui;
using Mapsui.Projections;
using Mapsui.Tiling;
using Mapsui.Layers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Mapsui.Nts;
using NetTopologySuite.Geometries;
using TourGuideAPP.Services;

namespace TourGuideAPP.Views;

public partial class MapPage : ContentPage
{
    private readonly LocationService _locationService;
    private readonly POIService _poiService;
    private readonly GeofenceEngine _geofenceEngine;
    private readonly NarrationService _narrationService;
    private string? _lastSpokenPOIId;
    private readonly (double lat, double lon, string? name)? _destination;
    private readonly HttpClient _http = new();

    public MapPage(LocationService locationService, POIService poiService,
                   GeofenceEngine geofenceEngine, NarrationService narrationService)
    {
        InitializeComponent();
        _locationService = locationService;
        _poiService = poiService;
        _geofenceEngine = geofenceEngine;
        _narrationService = narrationService;
    }

    public MapPage(LocationService locationService, POIService poiService,
                   GeofenceEngine geofenceEngine, NarrationService narrationService,
                   double destinationLat, double destinationLon, string? destinationName = null)
        : this(locationService, poiService, geofenceEngine, narrationService)
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
                    NearestPOILabel.Text = $"🏛️ Gần: {nearest.Name}";
                    if (_lastSpokenPOIId != nearest.Id)
                    {
                        _lastSpokenPOIId = nearest.Id;
                        nearest.LastPlayedAt = DateTime.Now;
                        await _narrationService.SpeakAsync(nearest.TtsScript);
                    }
                }
                else
                {
                    NearestPOILabel.Text = "🏛️ Chưa xác định điểm gần nhất";
                    _lastSpokenPOIId = null;
                }
            });
        };
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