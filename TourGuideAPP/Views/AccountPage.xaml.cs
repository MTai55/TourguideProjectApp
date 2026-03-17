using TourGuideAPP.Services;

namespace TourGuideAPP.Views;

public partial class AccountPage : ContentPage
{
    private readonly AuthService _authService;

    public AccountPage(AuthService authService)
    {
        InitializeComponent();
        _authService = authService;
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
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new LoginPage(_authService));
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        await _authService.LogoutAsync();
        UpdateUi();
        await Shell.Current.GoToAsync("//MainPage");
    }

    private async void OnComingSoon(object sender, EventArgs e)
    {
        await DisplayAlertAsync("Sắp có", "Tính năng đang được phát triển.", "OK");
    }
}

