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
    private readonly AuthService _authService;

    public TourDetailPage(
        ToursPage.TourCard tour,
        LocationService locationService,
        POIService poiService,
        GeofenceEngine geofenceEngine,
        NarrationService narrationService,
        AuthService authService)
    {
        InitializeComponent();
        _tour = tour;
        _locationService = locationService;
        _poiService = poiService;
        _geofenceEngine = geofenceEngine;
        _narrationService = narrationService;
        _authService = authService;

        TitleLabel.Text = tour.Title;
        MetaLabel.Text = $"{tour.DurationText} • {tour.BudgetText} • {tour.StopsText}";
        StopsCollection.ItemsSource = tour.Stops;
    }

    private void OnBackClicked(object sender, TappedEventArgs e)
    {
        Navigation.PopAsync();
    }

    private async void OnStopDirectionsClicked(object sender, TappedEventArgs e)
    {
        var placeId = e.Parameter?.ToString();
        var place = _tour.Stops.FirstOrDefault(p => p.PlaceId.ToString() == placeId);
        if (place is null) return;

        MapPage.PendingRoute = (place.Latitude, place.Longitude, place.Name);
        await Shell.Current.GoToAsync("//MainTabs/MapPage");
    }

    private async void OnStartClicked(object sender, TappedEventArgs e)
    {
        var first = _tour.Stops.FirstOrDefault();
        if (first is null)
        {
            await DisplayAlertAsync("Tour", "Tour này chưa có điểm dừng.", "OK");
            return;
        }

        MapPage.PendingRoute = (first.Latitude, first.Longitude, first.Name);
        await Shell.Current.GoToAsync("//MainTabs/MapPage");
    }
}
