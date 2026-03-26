using TourGuideAPP.Data.Models;
using TourGuideAPP.Services;

namespace TourGuideAPP.Views;

public partial class PlaceDetailPage : ContentPage
{
    private readonly Place _place;
    private readonly AuthService _authService;
    private readonly LocationService _locationService;
    private readonly POIService _poiService;
    private readonly GeofenceEngine _geofenceEngine;
    private readonly NarrationService _narrationService;
    private readonly UserProfileService _profileService;

    // Nhận Place object và AuthService từ trang trước truyền vào
    public PlaceDetailPage(
        Place place,
        AuthService authService,
        LocationService locationService,
        POIService poiService,
        GeofenceEngine geofenceEngine,
        NarrationService narrationService,
        UserProfileService profileService)
    {
        InitializeComponent();
        _place = place;
        _authService = authService;
        _locationService = locationService;
        _poiService = poiService;
        _geofenceEngine = geofenceEngine;
        _narrationService = narrationService;
        _profileService = profileService;

        LoadPlaceDetail();
    }

    // Hiển thị thông tin địa điểm lên UI
    private void LoadPlaceDetail()
    {
        // Ảnh bìa
        MainImage.Source = _place.ImageUrl;

        // Tên
        NameLabel.Text = _place.Name;

        // Rating
        RatingLabel.Text = _place.AverageRating.HasValue
            ? $"⭐ {_place.AverageRating:F1}"
            : "Chưa có đánh giá";

        // Số lượt đánh giá
        ReviewsLabel.Text = $"({_place.TotalReviews ?? 0} đánh giá)";

        // Mô tả
        DescriptionLabel.Text = _place.Description ?? "Chưa có mô tả";

        // Địa chỉ
        AddressLabel.Text = _place.Address ?? "Chưa cập nhật";

        // Giờ mở cửa
        OpenTimeLabel.Text = _place.OpenTime != null
            ? $"{_place.OpenTime} - {_place.CloseTime}"
            : "Chưa cập nhật";

        // SĐT
        PhoneLabel.Text = _place.Phone ?? "Chưa cập nhật";

        // Giá
        PriceLabel.Text = _place.PriceMin.HasValue
            ? $"{_place.PriceMin:N0}đ - {_place.PriceMax:N0}đ"
            : "Liên hệ";

        // Website
        WebsiteLabel.Text = _place.Website ?? "Chưa cập nhật";
    }

    // Mở MapPage và chỉ đường đến địa điểm hiện tại
    private async void OnDirectionsClicked(object sender, EventArgs e)
    {
        if (_place.Latitude == 0 && _place.Longitude == 0)
        {
            await DisplayAlert("Lỗi", "Không có tọa độ địa điểm để chỉ đường.", "OK");
            return;
        }

        var mapPage = new MapPage(
            _locationService,
            _poiService,
            _geofenceEngine,
            _narrationService,
            _profileService,
            _authService,
            _place.Latitude,
            _place.Longitude,
            _place.Name);

        await Navigation.PushAsync(mapPage);
    }

    // Đánh giá yêu cầu đăng nhập
    private async void OnReviewClicked(object sender, EventArgs e)
    {
        if (!_authService.IsLoggedIn)
        {
            await DisplayAlert("Thông báo", "Vui lòng đăng nhập để đánh giá.", "OK");
            return;
        }
        // Sau này làm ReviewPage
        await DisplayAlertAsync("Đánh giá", "Tính năng đang phát triển!", "OK");
    }

}


