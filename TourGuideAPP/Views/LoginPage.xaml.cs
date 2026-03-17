using TourGuideAPP.Services;

namespace TourGuideAPP.Views;

public partial class LoginPage : ContentPage
{
    private readonly AuthService _authService;

    public LoginPage(AuthService authService)
    {
        InitializeComponent();
        _authService = authService;
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        var email = EmailEntry.Text?.Trim();
        var password = PasswordEntry.Text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ErrorLabel.Text = "Vui lòng nhập đầy đủ thông tin!";
            ErrorLabel.IsVisible = true;
            return;
        }

        ErrorLabel.IsVisible = false;

        var success = await _authService.LoginAsync(email, password);

        if (success)
            await Shell.Current.GoToAsync("//MainPage");
        else
        {
            ErrorLabel.Text = "Email hoặc mật khẩu không đúng!";
            ErrorLabel.IsVisible = true;
        }
    }

    private async void OnGuestClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//MainPage");
    }

    private async void OnRegisterTapped(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new RegisterPage(_authService));
    }
}