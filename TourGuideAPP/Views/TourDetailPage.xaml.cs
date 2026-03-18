using TourGuideAPP.Data.Models;
using TourGuideAPP.Services;

namespace TourGuideAPP.Views;

public partial class TourDetailPage : ContentPage
{
    private readonly ToursPage.TourCard _tour;
    private readonly LocationService _locationService;
    private readonly POIService _poiService;
    private readonly GeofenceEngine _geofenceEngine;
    private readonly NarrationService _narrationService;

    public TourDetailPage(
        ToursPage.TourCard tour,
        LocationService locationService,
        POIService poiService,
        GeofenceEngine geofenceEngine,
        NarrationService narrationService)
    {
        InitializeComponent();
        _tour = tour;
        _locationService = locationService;
        _poiService = poiService;
        _geofenceEngine = geofenceEngine;
        _narrationService = narrationService;

        TitleLabel.Text = tour.Title;
        MetaLabel.Text = $"{tour.DurationText} • {tour.BudgetText} • {tour.StopsText}";
        StopsCollection.ItemsSource = tour.Stops;
    }

    private async void OnStartClicked(object sender, EventArgs e)
    {
        var first = _tour.Stops.FirstOrDefault();
        if (first is null)
        {
            await DisplayAlertAsync("Tour", "Tour này chưa có điểm dừng.", "OK");
            return;
        }

        await Navigation.PushAsync(new MapPage(
            _locationService,
            _poiService,
            _geofenceEngine,
            _narrationService,
            first.Latitude,
            first.Longitude,
            first.Name));
    }
}

