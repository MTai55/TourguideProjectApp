using ZXing.Net.Maui;
using TourGuideAPP.Services;

namespace TourGuideAPP.Views;

public partial class QRScanPage : ContentPage
{
    private readonly NarrationService _narrationService;
    private bool _isProcessing = false;

    public QRScanPage(NarrationService narrationService)
    {
        InitializeComponent();
        _narrationService = narrationService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Permissions.RequestAsync<Permissions.Camera>();
        BarcodeReader.Options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormats.TwoDimensional,
            AutoRotate = true,
            Multiple = false
        };
        BarcodeReader.IsDetecting = true;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        BarcodeReader.IsDetecting = false;
    }

    private async void OnBarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        if (_isProcessing) return;
        _isProcessing = true;

        var result = e.Results.FirstOrDefault();
        if (result == null) return;

        var text = result.Value;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            BarcodeReader.IsDetecting = false;
            ResultLabel.Text = $"✅ Đã quét: {text}";
            await _narrationService.SpeakAsync(text);

            await Task.Delay(3000);
            BarcodeReader.IsDetecting = true;
            _isProcessing = false;
        });
    }
}