using TourGuideAPP.Services;
using TourGuideAPP.Views;
using TourGuideAPP.Resources.Strings;

namespace TourGuideAPP;

public partial class App : Application
{
    private readonly AccessSessionService _accessService;
    private readonly IServiceProvider _services;

    public App(AccessSessionService accessService, IServiceProvider services)
    {
        // Apply saved language TRƯỚC khi InitializeComponent để XAML dùng đúng culture
        if (LocalizationService.IsLanguageSelected)
            LocalizationService.ApplySaved();

        InitializeComponent();
        _accessService = accessService;
        _services      = services;

        _accessService.AccessExpired += OnAccessExpired;

        if (_accessService.IsAccessValid())
            _accessService.StartExpiryTimer();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Đã có access hợp lệ → vào thẳng app
        if (_accessService.IsAccessValid())
            return new Window(new AppShell());

        // Chưa có access → luôn hiện chọn ngôn ngữ trước khi thanh toán
        var langPage = _services.GetRequiredService<LanguageSelectionPage>();
        return new Window(new NavigationPage(langPage)
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
                AppResources.AlertExpiredTitle,
                AppResources.AlertExpiredMsg,
                AppResources.AlertExpiredBtn);

            var langPage = _services.GetRequiredService<LanguageSelectionPage>();
            MainPage = new NavigationPage(langPage)
            {
                BarBackgroundColor = Color.FromArgb("#1A1410"),
                BarTextColor       = Color.FromArgb("#F0E6D3")
            };
        });
    }
}
