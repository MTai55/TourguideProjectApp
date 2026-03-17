using Microsoft.Extensions.DependencyInjection;
using TourGuideAPP.Services;
using TourGuideAPP.Views;

namespace TourGuideAPP;

public partial class App : Application
{
    private readonly AuthService _authService;

    // Inject AuthService để kiểm tra trạng thái đăng nhập khi khởi động
    public App(AuthService authService)
    {
        InitializeComponent();
        _authService = authService;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Tạo cửa sổ với AppShell làm root navigation
        var window = new Window(new AppShell());

        // Sau khi window tạo xong, điều hướng đến trang phù hợp
        window.Created += async (s, e) =>
        {
            await Task.Delay(100); // Đợi Shell khởi tạo xong

        };

        return window;
    }
}