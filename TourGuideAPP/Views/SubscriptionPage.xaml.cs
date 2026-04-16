using TourGuideAPP.Data.Models;
using TourGuideAPP.Resources.Strings;
using TourGuideAPP.Services;

namespace TourGuideAPP.Views;

public partial class SubscriptionPage : ContentPage
{
    private readonly AccessSessionService _accessService;
    private List<AccessPackage> _packages = new();

    public SubscriptionPage(AccessSessionService accessService)
    {
        InitializeComponent();
        _accessService = accessService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadPackagesAsync();
    }

    private async Task LoadPackagesAsync()
    {
        _packages = await _accessService.GetPackagesAsync();

        // Map PackageId → price label
        var priceMap = new Dictionary<string, Label>
        {
            ["1h"]   = Price1h,
            ["2h"]   = Price2h,
            ["1day"] = Price1day,
            ["3day"] = Price3day,
        };

        foreach (var pkg in _packages)
        {
            if (priceMap.TryGetValue(pkg.PackageId, out var label))
                label.Text = FormatPrice(pkg.PriceVnd);
        }
    }

    private static string FormatPrice(int priceVnd)
        => $"{priceVnd:N0}đ".Replace(",", ".");

    private AccessPackage? GetPackage(string packageId)
        => _packages.FirstOrDefault(p => p.PackageId == packageId);

    private async void OnPackage1hTapped(object sender, TappedEventArgs e)
    {
        var pkg = GetPackage("1h");
        if (pkg != null) await GoToPayment(pkg);
    }

    private async void OnPackage2hTapped(object sender, TappedEventArgs e)
    {
        var pkg = GetPackage("2h");
        if (pkg != null) await GoToPayment(pkg);
    }

    private async void OnPackage1dayTapped(object sender, TappedEventArgs e)
    {
        var pkg = GetPackage("1day");
        if (pkg != null) await GoToPayment(pkg);
    }

    private async void OnPackage3dayTapped(object sender, TappedEventArgs e)
    {
        var pkg = GetPackage("3day");
        if (pkg != null) await GoToPayment(pkg);
    }

    private async Task GoToPayment(AccessPackage pkg)
    {
        await Navigation.PushAsync(new PaymentQRPage(pkg, _accessService));
    }

    // DEV ONLY: xóa trước khi release
    private void OnDevBypassTapped(object sender, TappedEventArgs e)
    {
        var expiresAt = DateTime.UtcNow.AddHours(999);
        Preferences.Set("access_expires_at", expiresAt.ToString("O"));
        _accessService.StartExpiryTimer();
        Application.Current!.MainPage = new AppShell();
    }
}
