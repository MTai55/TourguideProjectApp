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

    private void UpdateUi()
    {
        var loggedIn = _authService.IsLoggedIn;
        StatusLabel.Text = loggedIn ? "👤 Đã đăng nhập" : "👤 Khách";
        LoginBtn.IsVisible = !loggedIn;
        LogoutBtn.IsVisible = loggedIn;

        SummaryLabel.Text = "Các tính năng đã bị vô hiệu hóa";
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

}


