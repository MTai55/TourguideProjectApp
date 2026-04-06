using TourGuideAPP.Services;
using TourGuideAPP.Views;

namespace TourGuideAPP;

public partial class App : Application
{
    private readonly AccessSessionService _accessService;
    private readonly IServiceProvider _services;

    public App(AccessSessionService accessService, IServiceProvider services)
    {
        InitializeComponent();
        _accessService = accessService;
        _services      = services;

        // Lắng nghe sự kiện hết hạn từ bất kỳ đâu trong app
        _accessService.AccessExpired += OnAccessExpired;

        // Nếu đang có session hợp lệ, khởi động timer ngay
        if (_accessService.IsAccessValid())
            _accessService.StartExpiryTimer();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        if (_accessService.IsAccessValid())
            return new Window(new AppShell());

        // Chưa có gói → hiện trang chọn gói
        var subPage = _services.GetRequiredService<SubscriptionPage>();
        return new Window(new NavigationPage(subPage)
        {
            BarBackgroundColor = Color.FromArgb("#1A1410"),
            BarTextColor       = Color.FromArgb("#F0E6D3")
        });
    }

    private void OnAccessExpired()
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await MainPage!.DisplayAlert(
                "Hết hạn sử dụng",
                "Gói sử dụng của bạn đã hết hạn.\nVui lòng gia hạn để tiếp tục khám phá.",
                "Gia hạn ngay");

            var subPage = _services.GetRequiredService<SubscriptionPage>();
            MainPage = new NavigationPage(subPage)
            {
                BarBackgroundColor = Color.FromArgb("#1A1410"),
                BarTextColor       = Color.FromArgb("#F0E6D3")
            };
        });
    }
}
