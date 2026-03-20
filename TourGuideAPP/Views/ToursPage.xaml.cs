using System.Collections.ObjectModel;
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
            "1–1.5 giờ",
            "2–3 giờ",
            "Nửa ngày"
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

    private async void OnSelectTourClicked(object sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not string id)
            return;

        var selected = Tours.FirstOrDefault(t => t.Id == id);
        if (selected is null)
        {
            await DisplayAlertAsync("Chọn tour", "Không tìm thấy tour.", "OK");
            return;
        }

        if (selected.Stops.Count == 0)
        {
            await DisplayAlertAsync("Chọn tour", "Tour demo chưa có điểm dừng để xem chi tiết.", "OK");
            return;
        }

        var sp = Handler?.MauiContext?.Services;
        var locationService = sp?.GetService<LocationService>();
        var poiService = sp?.GetService<POIService>();
        var geofenceEngine = sp?.GetService<GeofenceEngine>();
        var narrationService = sp?.GetService<NarrationService>();

        var profileService = sp?.GetService<UserProfileService>();
        var authService = sp?.GetService<AuthService>();

        if (locationService is null || poiService is null || geofenceEngine is null || narrationService is null || profileService is null || authService is null)
        {
            await DisplayAlertAsync("Thiếu dịch vụ", "Không khởi tạo được các dịch vụ để mở tour chi tiết.", "OK");
            return;
        }

        await Navigation.PushAsync(new TourDetailPage(selected, locationService, poiService, geofenceEngine, narrationService, profileService, authService));
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
                "Chưa tìm thấy tour phù hợp",
                "Thử tăng ngân sách hoặc xóa từ khóa tìm kiếm để xem thêm gợi ý.",
                DurationTextFromBucket(_filters.DurationBucket),
                $"≤ {_filters.MaxBudgetK}k",
                "Gợi ý",
                "• 0 điểm",
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
        Tours.Add(BuildTour("tour-quick", "Tour nhanh", "Đi ít điểm nhưng chọn chỗ “đáng” nhất.", stopCount: Math.Min(2, top.Count), top));
        Tours.Add(BuildTour("tour-balanced", "Tour cân bằng", "Phù hợp đa số: vừa đủ điểm, dễ đi.", stopCount: Math.Min(stopCount, top.Count), top));
        Tours.Add(BuildTour("tour-full", "Tour no nê", "Nhiều điểm hơn nếu bạn có thời gian.", stopCount: Math.Min(stopCount + 1, top.Count), top));
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
        var tag = _filters.PreferLowWalking ? "Đi bộ ít" : "Gợi ý";
        var stopsText = $"• {stops.Count} điểm";

        if (_filters.PreferLowWalking)
            desc = "Ưu tiên lộ trình ngắn, ít di chuyển.";

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
        0 => "1–1.5 giờ",
        2 => "Nửa ngày",
        _ => "2–3 giờ"
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

