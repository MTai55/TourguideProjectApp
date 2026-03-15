using Mapsui;
using Mapsui.Projections;
using Mapsui.Tiling;
using Mapsui.Layers;
using TourGuideAPP.Services;

namespace TourGuideAPP.Views;

public partial class MapPage : ContentPage
{
    private readonly LocationService _locationService;
    private readonly POIService _poiService;
    private readonly GeofenceEngine _geofenceEngine;
    private readonly NarrationService _narrationService;
    private string? _lastSpokenPOIId;

    public MapPage(LocationService locationService, POIService poiService,
                   GeofenceEngine geofenceEngine, NarrationService narrationService)
    {
        InitializeComponent();
        _locationService = locationService;
        _poiService = poiService;
        _geofenceEngine = geofenceEngine;
        _narrationService = narrationService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        SetupMap();
        await LoadPOIsAsync();
        StartGPS();
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
                    NearestPOILabel.Text = "🏛️ Không có POI gần đây";
                    _lastSpokenPOIId = null;
                }
            });
        };
    }
}