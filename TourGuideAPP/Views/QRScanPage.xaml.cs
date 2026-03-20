using ZXing.Net.Maui;
using TourGuideAPP.Services;

namespace TourGuideAPP.Views;

public partial class QRScanPage : ContentPage
{
    private readonly NarrationService _narrationService;
    private readonly UserProfileService _profileService;
    private readonly PlaceService _placeService;
    private bool _isProcessing = false;

    public QRScanPage(NarrationService narrationService, UserProfileService profileService, PlaceService placeService)
    {
        InitializeComponent();
        _narrationService = narrationService;
        _profileService = profileService;
        _placeService = placeService;
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

            // QR code có thể là ID place (số) hoặc đường dẫn/chuỗi dữ liệu.
            if (int.TryParse(text, out var placeId))
            {
                var place = _placeService.GetCachedPlaces().FirstOrDefault(p => p.PlaceId == placeId);
                if (place != null)
                {
                    await _profileService.AddHistoryByQRAsync(place);
                    await DisplayAlert("Check-in QR", $"Đã ghi nhận bạn đã đến {place.Name} qua QR code", "OK");
                }
                else
                {
                    await DisplayAlert("QR không hợp lệ", "Không tìm thấy địa điểm với mã QR này", "OK");
                }
            }
            else
            {
                await DisplayAlert("QR đã quét", "Mã QR không chứa placeId, chỉ ghi nhận raw text", "OK");
            }

            await Task.Delay(3000);
            BarcodeReader.IsDetecting = true;
            _isProcessing = false;
        });
    }
}