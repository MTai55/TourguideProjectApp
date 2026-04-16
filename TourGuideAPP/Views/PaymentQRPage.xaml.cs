using TourGuideAPP.Data.Models;
using TourGuideAPP.Resources.Strings;
using TourGuideAPP.Services;

namespace TourGuideAPP.Views;

public partial class PaymentQRPage : ContentPage
{
    private readonly AccessPackage _package;
    private readonly AccessSessionService _accessService;

    public PaymentQRPage(AccessPackage package, AccessSessionService accessService)
    {
        InitializeComponent();
        _package       = package;
        _accessService = accessService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var deviceId = _accessService.GetDeviceId();

        // Hiển thị thông tin gói
        PackageNameLabel.Text  = _package.PackageId.ToUpper();
        PackagePriceLabel.Text = $"{_package.PriceVnd:N0}đ".Replace(",", ".");
        DeviceIdLabel.Text     = deviceId;

        // Load QR từ VietQR
        var addInfo  = Uri.EscapeDataString($"TGAPP {deviceId}");
        var accName  = Uri.EscapeDataString(Constants.BankAccountName);
        var qrUrl    = $"https://img.vietqr.io/image/{Constants.BankId}-{Constants.BankAccount}-compact2.png" +
                       $"?amount={_package.PriceVnd}&addInfo={addInfo}&accountName={accName}";
        QRImage.Source = ImageSource.FromUri(new Uri(qrUrl));

        // Tạo session pending trên Supabase rồi bắt đầu polling
        try
        {
            var sessionId = await _accessService.CreatePendingSessionAsync(
                _package.PackageId, _package.DurationHours, _package.PriceVnd);

            _accessService.StartPollingForActivation(sessionId, OnPaymentActivated);
        }
        catch
        {
            StatusLabel.Text   = AppResources.PayConnError;
            WaitingSpinner.IsRunning = false;
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _accessService.StopPolling();
    }

    private void OnPaymentActivated()
    {
        // Khởi động timer kiểm tra hết hạn rồi mở app chính
        _accessService.StartExpiryTimer();
        Application.Current!.MainPage = new AppShell();
    }
}
