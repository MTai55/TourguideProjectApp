using TourGuideAPP.Services;

namespace TourGuideAPP.Views;

public record SubscriptionPackage(string Id, string Label, double Hours, int Price);

public partial class SubscriptionPage : ContentPage
{
    private readonly AccessSessionService _accessService;

    private static readonly List<SubscriptionPackage> Packages = new()
    {
        new("1h",   "1 Tiếng",  1,   10_000),
        new("2h",   "2 Tiếng",  2,   18_000),
        new("1day", "1 Ngày",  24,   50_000),
        new("3day", "3 Ngày",  72,  120_000),
    };

    public SubscriptionPage(AccessSessionService accessService)
    {
        InitializeComponent();
        _accessService = accessService;
    }

    private async void OnPackage1hTapped(object sender, TappedEventArgs e)
        => await GoToPayment(Packages[0]);

    private async void OnPackage2hTapped(object sender, TappedEventArgs e)
        => await GoToPayment(Packages[1]);

    private async void OnPackage1dayTapped(object sender, TappedEventArgs e)
        => await GoToPayment(Packages[2]);

    private async void OnPackage3dayTapped(object sender, TappedEventArgs e)
        => await GoToPayment(Packages[3]);

    private async Task GoToPayment(SubscriptionPackage package)
    {
        await Navigation.PushAsync(new PaymentQRPage(package, _accessService));
    }

    // DEV ONLY: xóa trước khi release
    private void OnDevBypassTapped(object sender, TappedEventArgs e)
    {
        // Set session giả 999 giờ không cần Supabase
        var expiresAt = DateTime.UtcNow.AddHours(999);
        Preferences.Set("access_expires_at", expiresAt.ToString("O"));
        _accessService.StartExpiryTimer();
        Application.Current!.MainPage = new AppShell();
    }
}
