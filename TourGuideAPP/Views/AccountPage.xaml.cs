using TourGuideAPP.Services;

namespace TourGuideAPP.Views;

public partial class AccountPage : ContentPage
{
    private readonly AuthService _authService;
    private readonly UserProfileService _profileService;

    public AccountPage(AuthService authService, UserProfileService profileService)
    {
        InitializeComponent();
        _authService = authService;
        _profileService = profileService;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateUi();
    }

    private async void UpdateUi()
    {
        var loggedIn = _authService.IsLoggedIn;
        StatusLabel.Text = loggedIn ? "👤 Đã đăng nhập" : "👤 Khách";
        LoginBtn.IsVisible = !loggedIn;
        LogoutBtn.IsVisible = loggedIn;

        var favorites = await _profileService.GetFavoritesAsync();
        var history = await _profileService.GetTripHistoryAsync();
        var notes = await _profileService.GetNotesAsync();
        var wishlist = await _profileService.GetWishlistAsync();

        SummaryLabel.Text = $"Yêu thích: {favorites.Count} | Lịch sử: {history.Count} | Ghi chú: {notes.Count} | Wishlist: {wishlist.Count}";
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//LoginPage");
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        await _authService.LogoutAsync();
        UpdateUi();
        // Giữ trên AccountPage, không chuyển sang LoginPage tự động.
        await DisplayAlertAsync("Đăng xuất", "Bạn đã đăng xuất. Vẫn đang ở chế độ khách.", "OK");
    }

    private async void OnFavoritesClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("FavoritesPage");
    }

    private async void OnHistoryClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("TripHistoryPage");
    }

    private async void OnNotesClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("NotesPage");
    }

    private async void OnWishlistClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("WishlistPage");
    }
}


