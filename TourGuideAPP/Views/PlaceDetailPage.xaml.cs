using Microsoft.Maui.ApplicationModel.Communication;
using TourGuideAPP.Data.Models;
using TourGuideAPP.Services;

namespace TourGuideAPP.Views;

public partial class PlaceDetailPage : ContentPage
{
    private readonly Place _place;
    private readonly AuthService _authService;
    private readonly LocationService _locationService;
    private readonly GeofenceEngine _geofenceEngine;
    private readonly NarrationService _narrationService;
    private readonly UserProfileService _profileService;

    public PlaceDetailPage(
        Place place,
        AuthService authService,
        LocationService locationService,
        GeofenceEngine geofenceEngine,
        NarrationService narrationService,
        UserProfileService profileService)
    {
        InitializeComponent();
        _place = place;
        _authService = authService;
        _locationService = locationService;
        _geofenceEngine = geofenceEngine;
        _narrationService = narrationService;
        _profileService = profileService;

        LoadPlaceDetail();
    }

    private void LoadPlaceDetail()
    {
        MainImage.Source = _place.ImageUrl;
        NameLabel.Text = _place.Name;

        RatingLabel.Text = _place.AverageRating.HasValue
            ? $"★ {_place.AverageRating:F1}"
            : "Chưa có đánh giá";
        ReviewsLabel.Text = _place.TotalReviews.HasValue
            ? $"({_place.TotalReviews} đánh giá Google)"
            : "";

        // Status badge
        bool isOpen = IsPlaceOpen(_place);
        StatusLabel.Text = isOpen ? "Đang mở" : "Đóng cửa";
        StatusLabel.TextColor = isOpen ? Color.FromArgb("#4CAF50") : Color.FromArgb("#E94560");
        StatusBadge.BackgroundColor = isOpen ? Color.FromArgb("#1B3A28") : Color.FromArgb("#3A1B20");

        DescriptionLabel.Text = _place.Description ?? "Chưa có mô tả.";
        AddressLabel.Text = _place.Address ?? "Chưa cập nhật";
        OpenTimeLabel.Text = _place.OpenTime != null
            ? $"{_place.OpenTime} – {_place.CloseTime}"
            : "Chưa cập nhật";
        PriceLabel.Text = _place.PriceMin.HasValue
            ? $"{_place.PriceMin:N0}đ – {_place.PriceMax:N0}đ"
            : "Liên hệ";

        if (!string.IsNullOrWhiteSpace(_place.Website))
            WebsiteLabel.Text = _place.Website;
        else
            WebsiteRow.IsVisible = false;
    }

    private static bool IsPlaceOpen(Place place)
    {
        if (place.OpenTime is null || place.CloseTime is null) return false;
        if (!TimeSpan.TryParse(place.OpenTime, out var open) ||
            !TimeSpan.TryParse(place.CloseTime, out var close)) return false;
        var now = DateTime.Now.TimeOfDay;
        return now >= open && now <= close;
    }

    private void OnBackClicked(object sender, TappedEventArgs e)
        => Navigation.PopAsync();

    private async void OnDirectionsClicked(object sender, TappedEventArgs e)
    {
        if (_place.Latitude == 0 && _place.Longitude == 0)
        {
            await DisplayAlert("Lỗi", "Không có tọa độ để chỉ đường.", "OK");
            return;
        }
        MapPage.PendingRoute = (_place.Latitude, _place.Longitude, _place.Name);
        await Shell.Current.GoToAsync("//MainTabs/MapPage");
    }

    private async void OnGoogleMapsClicked(object sender, TappedEventArgs e)
    {
        // Deep link ưu tiên: tìm theo tên + tọa độ → mở Google Maps app
        // Nếu chưa cài Google Maps thì fallback sang trình duyệt
        var name = Uri.EscapeDataString(_place.Name ?? "");
        var lat = _place.Latitude;
        var lon = _place.Longitude;

        // geo URI → mở Google Maps app trực tiếp (Android)
        var geoUri = $"geo:{lat},{lon}?q={lat},{lon}({name})";

        // Fallback URL cho trình duyệt
        var webUrl = $"https://www.google.com/maps/search/?api=1&query={lat},{lon}";

        try
        {
            await Launcher.OpenAsync(new Uri(geoUri));
        }
        catch
        {
            // Thiết bị không có Google Maps → mở trình duyệt
            await Launcher.OpenAsync(new Uri(webUrl));
        }
    }

    private void OnCallClicked(object sender, TappedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_place.Phone))
        {
            DisplayAlert("Thông báo", "Địa điểm này chưa có số điện thoại.", "OK");
            return;
        }
        try { PhoneDialer.Open(_place.Phone); }
        catch { DisplayAlert("Lỗi", "Không thể mở ứng dụng gọi điện.", "OK"); }
    }

    private async void OnWebsiteClicked(object sender, TappedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_place.Website)) return;
        try { await Launcher.OpenAsync(new Uri(_place.Website)); }
        catch { await DisplayAlert("Lỗi", "Không thể mở website.", "OK"); }
    }
}
