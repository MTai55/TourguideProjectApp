using System.Collections.ObjectModel;
using TourGuideAPP.Resources.Strings;
using TourGuideAPP.Services;
using TourGuideAPP.Data.Models;

namespace TourGuideAPP.Views;

public partial class ToursPage : ContentPage
{
    public ObservableCollection<TourCard> Tours { get; } = new();

    private readonly List<Place> _places = new();
    private TourFilters _filters = TourFilters.Default;

    public ToursPage()
    {
        InitializeComponent();
        BindingContext = this;

        DurationPicker.ItemsSource = new List<string>
        {
            AppResources.TourDuration1,
            AppResources.TourDuration2,
            AppResources.TourDurationHalfDay
        };
        DurationPicker.SelectedIndex = 1;
        UpdateBudgetLabel();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await EnsurePlacesLoadedAsync();
        RebuildTours();
    }

    private async Task EnsurePlacesLoadedAsync()
    {
        if (_places.Count > 0)
            return;

        var sp = Handler?.MauiContext?.Services;
        var placeService = sp?.GetService<PlaceService>();

        if (placeService is null)
        {
            // Fallback: show demo tours if DI not available.
            if (Tours.Count == 0)
            {
                Tours.Add(new TourCard("demo-1", "Foodie Khánh Hội", "Đi bộ nhẹ nhàng, ăn ngon, nhiều điểm check-in.", "2–3 giờ", "100k–250k", "Ăn vặt", "• 3 điểm", Array.Empty<Place>()));
                Tours.Add(new TourCard("demo-2", "Cafe & Chill", "Ưu tiên quán đẹp – yên tĩnh, ít di chuyển.", "2 giờ", "80k–200k", "Cafe", "• 2 điểm", Array.Empty<Place>()));
            }
            return;
        }

        var places = await placeService.GetAllPlacesAsync();
        _places.Clear();
        _places.AddRange(places.Where(p => p.IsActive && p.IsApproved));
    }

    private void OnFilterClicked(object sender, EventArgs e)
    {
        FilterPanel.IsVisible = !FilterPanel.IsVisible;
    }

    private void OnBudgetChanged(object sender, ValueChangedEventArgs e)
    {
        UpdateBudgetLabel();
    }

    private void UpdateBudgetLabel()
    {
        var v = (int)Math.Round(BudgetSlider.Value);
        BudgetValueLabel.Text = $"{v}k";
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _filters = _filters with { Query = (e.NewTextValue ?? "").Trim() };
        RebuildTours();
    }

    private void OnApplyFiltersClicked(object sender, EventArgs e)
    {
        _filters = new TourFilters(
            Query: (SearchEntry.Text ?? string.Empty).Trim(),
            DurationBucket: DurationPicker.SelectedIndex,
            MaxBudgetK: (int)Math.Round(BudgetSlider.Value),
            PreferLowWalking: LowWalkingSwitch.IsToggled
        );

        RebuildTours();
        FilterPanel.IsVisible = false;
    }

    private async void OnSelectTourClicked(object sender, TappedEventArgs e)
    {
        var id = e.Parameter?.ToString();
        if (string.IsNullOrWhiteSpace(id)) return;

        var selected = Tours.FirstOrDefault(t => t.Id == id);
        if (selected is null) return;

        if (selected.Stops.Count == 0)
        {
            await DisplayAlertAsync(AppResources.AlertSelectTour, AppResources.AlertNoStops, AppResources.AlertOk);
            return;
        }

        var sp = Handler?.MauiContext?.Services;
        var locationService = sp?.GetService<LocationService>();
        var poiService = sp?.GetService<POIService>();
        var geofenceEngine = sp?.GetService<GeofenceEngine>();
        var narrationService = sp?.GetService<NarrationService>();
        var authService = sp?.GetService<AuthService>();

        if (locationService is null || poiService is null || geofenceEngine is null ||
            narrationService is null || authService is null)
        {
            await DisplayAlertAsync(AppResources.AlertError, AppResources.AlertServiceError, AppResources.AlertOk);
            return;
        }

        await Navigation.PushAsync(new TourDetailPage(selected, locationService, poiService,
                                                       geofenceEngine, narrationService,
                                                       authService));
    }

    private void RebuildTours()
    {
        Tours.Clear();

        if (_places.Count == 0)
            return;

        var candidates = ApplyPlaceFilters(_places, _filters).ToList();
        if (candidates.Count == 0)
        {
            Tours.Add(new TourCard(
                "empty",
                AppResources.TourEmptyTitle,
                AppResources.TourEmptyDesc,
                DurationTextFromBucket(_filters.DurationBucket),
                $"≤ {_filters.MaxBudgetK}k",
                AppResources.TourSuggestionTag,
                string.Format(AppResources.TourStopsFormat, 0),
                Array.Empty<Place>()));
            return;
        }

        var stopCount = StopsFromDurationBucket(_filters.DurationBucket);
        var top = candidates
            .OrderByDescending(p => p.AverageRating ?? 0)
            .ThenBy(p => p.PriceMin ?? decimal.MaxValue)
            .Take(Math.Max(2, stopCount + 1))
            .ToList();

        // Build 3 variations from the same candidate pool
        Tours.Add(BuildTour("tour-quick",    AppResources.TourQuickTitle,    AppResources.TourQuickDesc,    stopCount: Math.Min(2, top.Count),          top));
        Tours.Add(BuildTour("tour-balanced", AppResources.TourBalancedTitle, AppResources.TourBalancedDesc, stopCount: Math.Min(stopCount, top.Count),     top));
        Tours.Add(BuildTour("tour-full",     AppResources.TourFullTitle,     AppResources.TourFullDesc,     stopCount: Math.Min(stopCount + 1, top.Count), top));
    }

    private static IEnumerable<Place> ApplyPlaceFilters(IEnumerable<Place> places, TourFilters filters)
    {
        var q = filters.Query;
        if (!string.IsNullOrWhiteSpace(q))
        {
            var qLower = q.ToLowerInvariant();
            places = places.Where(p =>
                p.Name.ToLowerInvariant().Contains(qLower) ||
                (p.Description?.ToLowerInvariant().Contains(qLower) ?? false));
        }

        // Budget filter (k -> VND)
        var maxVnd = filters.MaxBudgetK * 1000m;
        places = places.Where(p =>
            !p.PriceMin.HasValue || p.PriceMin.Value <= maxVnd);

        return places;
    }

    private TourCard BuildTour(string id, string title, string desc, int stopCount, List<Place> pool)
    {
        var stops = pool.Take(stopCount).ToList();
        var budgetText = $"≤ {_filters.MaxBudgetK}k";
        var durationText = DurationTextFromBucket(_filters.DurationBucket);
        var tag = _filters.PreferLowWalking ? AppResources.TourWalkLess : AppResources.TourSuggestionTag;
        var stopsText = string.Format(AppResources.TourStopsFormat, stops.Count);

        if (_filters.PreferLowWalking)
            desc = AppResources.TourLowWalkDesc;

        return new TourCard(id, title, desc, durationText, budgetText, tag, stopsText, stops);
    }

    private static int StopsFromDurationBucket(int bucket) => bucket switch
    {
        0 => 2,
        2 => 5,
        _ => 3
    };

    private static string DurationTextFromBucket(int bucket) => bucket switch
    {
        0 => AppResources.TourDuration1,
        2 => AppResources.TourDurationHalfDay,
        _ => AppResources.TourDuration2
    };

    public sealed record TourCard(
        string Id,
        string Title,
        string Description,
        string DurationText,
        string BudgetText,
        string Tag,
        string StopsText,
        IReadOnlyList<Place> Stops);

    private sealed record TourFilters(
        string Query,
        int DurationBucket,
        int MaxBudgetK,
        bool PreferLowWalking)
    {
        public static TourFilters Default => new(Query: "", DurationBucket: 1, MaxBudgetK: 200, PreferLowWalking: false);
    }
}

