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
        UpdateActionButtons();
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

    // Mở Google Maps chỉ đường đến địa điểm
    private async void OnDirectionsClicked(object sender, EventArgs e)
    {
        if (Shell.Current is AppShell appShell)
        {
            appShell.ActivateMapTab();
        }
        else
        {
            await Shell.Current.GoToAsync("//MainTabs/MapPage");
        }

        // Nếu cần thêm dữ liệu đích (destination), có thể dùng service trung gian ở đây.
    }

    // Đánh giá yêu cầu đăng nhập
    private async void OnReviewClicked(object sender, EventArgs e)
    {
        if (!_authService.IsLoggedIn)
        {
            await Shell.Current.GoToAsync("//LoginPage");
            return;
        }
        // Sau này làm ReviewPage
        await DisplayAlertAsync("Đánh giá", "Tính năng đang phát triển!", "OK");
    }

    private async void OnFavoriteClicked(object sender, EventArgs e)
    {
        if (!_authService.IsLoggedIn)
        {
            await DisplayAlert("Yêu cầu đăng nhập", "Vui lòng đăng nhập để dùng tính năng này", "OK");
            await Shell.Current.GoToAsync("//LoginPage");
            return;
        }

        var favorites = await _profileService.GetFavoritesAsync();
        var existing = favorites.Any(x => x.PlaceId == _place.PlaceId);
        if (existing)
        {
            await _profileService.RemoveFavoriteAsync(_place.PlaceId);
            await DisplayAlert("Đã xoá", "Đã xoá khỏi danh sách yêu thích", "OK");
        }
        else
        {
            await _profileService.AddFavoriteAsync(_place);
            await DisplayAlert("Thêm vào yêu thích", "Đã thêm vào danh sách yêu thích", "OK");
        }

        UpdateActionButtons();
    }

    private async void OnCheckInClicked(object sender, EventArgs e)
    {
        if (!_authService.IsLoggedIn)
        {
            await DisplayAlert("Yêu cầu đăng nhập", "Vui lòng đăng nhập để dùng tính năng này", "OK");
            await Shell.Current.GoToAsync("//LoginPage");
            return;
        }

        var current = _locationService.LastKnownLocation;
        if (current is null)
        {
            await DisplayAlert("Lỗi GPS", "Chưa có vị trí GPS. Vui lòng bật GPS và thử lại.", "OK");
            return;
        }

        var distance = GetDistanceMeters(current.Latitude, current.Longitude, _place.Latitude, _place.Longitude);
        if (distance <= 150) // chạy check-in trong bán kính 150m
        {
            await _profileService.AddHistoryAsync(_place, "GPS");
            await DisplayAlert("Check-in thành công", $"Bạn đã được ghi nhận tại {_place.Name} (GPS) - {distance:F0}m.", "OK");
        }
        else
        {
            await DisplayAlert("Chưa đến nơi", $"Khoảng cách đến {_place.Name} là {distance:F0}m. Vui lòng đến gần hơn để check-in.", "OK");
        }
    }

    private async void OnBookingCompleteClicked(object sender, EventArgs e)
    {
        if (!_authService.IsLoggedIn)
        {
            await DisplayAlert("Yêu cầu đăng nhập", "Vui lòng đăng nhập để dùng tính năng này", "OK");
            await Shell.Current.GoToAsync("//LoginPage");
            return;
        }

        await _profileService.AddHistoryByBookingAsync(_place);
        await DisplayAlert("Hoàn thành đặt bàn", "Đã ghi lịch sử: đã đến quán sau đặt bàn thành công.", "OK");
    }

    private static double GetDistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        static double ToRad(double deg) => deg * Math.PI / 180;

        var R = 6371000; // meters
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private async void OnWishlistClicked(object sender, EventArgs e)
    {
        if (!_authService.IsLoggedIn)
        {
            await DisplayAlert("Yêu cầu đăng nhập", "Vui lòng đăng nhập để dùng tính năng này", "OK");
            await Shell.Current.GoToAsync("//LoginPage");
            return;
        }

        var wishlist = await _profileService.GetWishlistAsync();
        var existing = wishlist.Any(x => x.PlaceId == _place.PlaceId);
        if (existing)
        {
            await _profileService.RemoveWishlistAsync(_place.PlaceId);
            await DisplayAlert("Đã xoá", "Đã xoá khỏi wishlist", "OK");
        }
        else
        {
            await _profileService.AddWishlistAsync(_place);
            await DisplayAlert("Thêm vào wishlist", "Đã thêm vào wishlist", "OK");
        }

        UpdateActionButtons();
    }

    private async void UpdateActionButtons()
    {
        var favorites = await _profileService.GetFavoritesAsync();
        var isFav = favorites.Any(x => x.PlaceId == _place.PlaceId);
        FavoriteButton.Text = isFav ? "💔 Bỏ yêu thích" : "❤ Thêm yêu thích";

        var wishlist = await _profileService.GetWishlistAsync();
        var onWishlist = wishlist.Any(x => x.PlaceId == _place.PlaceId);
        WishlistButton.Text = onWishlist ? "🧾 Bỏ wishlist" : "💖 Thêm wishlist";
    }
}
