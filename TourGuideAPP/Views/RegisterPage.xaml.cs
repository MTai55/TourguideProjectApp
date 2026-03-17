using TourGuideAPP.Services;

namespace TourGuideAPP.Views;

public partial class RegisterPage : ContentPage
{
    private readonly AuthService _authService;

    public RegisterPage(AuthService authService)
    {
        InitializeComponent();
        _authService = authService;
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        var fullName = FullNameEntry.Text?.Trim();
        var email = EmailEntry.Text?.Trim();
        var password = PasswordEntry.Text;
        var confirmPassword = ConfirmPasswordEntry.Text;

        ErrorLabel.IsVisible = false;
        SuccessLabel.IsVisible = false;

        if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(email) ||
            string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
        {
            ErrorLabel.Text = "Vui lòng nhập đầy đủ thông tin!";
            ErrorLabel.IsVisible = true;
            return;
        }

        if (password != confirmPassword)
        {
            ErrorLabel.Text = "Mật khẩu xác nhận không khớp!";
            ErrorLabel.IsVisible = true;
            return;
        }

        if (password.Length < 6)
        {
            ErrorLabel.Text = "Mật khẩu phải có ít nhất 6 ký tự!";
            ErrorLabel.IsVisible = true;
            return;
        }

        var success = await _authService.RegisterAsync(email, password, fullName);

        if (success)
        {
            SuccessLabel.Text = "Đăng ký thành công! Vui lòng kiểm tra email.";
            SuccessLabel.IsVisible = true;
            await Task.Delay(2000);
            await Navigation.PopAsync();
        }
        else
        {
            ErrorLabel.Text = "Email đã tồn tại hoặc có lỗi xảy ra!";
            ErrorLabel.IsVisible = true;
        }
    }

    private async void OnLoginTapped(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}