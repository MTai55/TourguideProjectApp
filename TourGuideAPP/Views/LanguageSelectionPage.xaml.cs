using TourGuideAPP.Services;

namespace TourGuideAPP.Views;

public partial class LanguageSelectionPage : ContentPage
{
    private readonly AccessSessionService _accessService;
    private readonly IServiceProvider _services;
    private string _selectedLang = "vi"; // default

    public LanguageSelectionPage(AccessSessionService accessService, IServiceProvider services)
    {
        InitializeComponent();
        _accessService = accessService;
        _services      = services;

        // Pre-select ngôn ngữ đã lưu từ lần trước (nếu có)
        if (LocalizationService.IsLanguageSelected && LocalizationService.SavedLanguage == "en")
            SelectLanguage("en");
    }

    private void OnVietnameseTapped(object sender, TappedEventArgs e) => SelectLanguage("vi");
    private void OnEnglishTapped(object sender, TappedEventArgs e)    => SelectLanguage("en");

    private void SelectLanguage(string lang)
    {
        _selectedLang = lang;
        bool isEn = lang == "en";

        CardVi.Stroke          = new SolidColorBrush(Color.FromArgb(isEn ? "#2A2018" : "#C8A96E"));
        CardVi.StrokeThickness = isEn ? 1 : 1.5;
        CardEn.Stroke          = new SolidColorBrush(Color.FromArgb(isEn ? "#C8A96E" : "#2A2018"));
        CardEn.StrokeThickness = isEn ? 1.5 : 1;
        CheckVi.IsVisible      = !isEn;
        CheckEn.IsVisible      = isEn;
    }

    private void OnContinueTapped(object sender, TappedEventArgs e)
    {
        // Lưu và apply culture — tất cả pages tạo sau đây sẽ dùng đúng ngôn ngữ
        LocalizationService.SaveAndApply(_selectedLang);

        // LanguageSelectionPage chỉ hiện khi chưa có access
        // → luôn đi thẳng sang SubscriptionPage
        var subPage = _services.GetRequiredService<SubscriptionPage>();
        Application.Current!.MainPage = new NavigationPage(subPage)
        {
            BarBackgroundColor = Color.FromArgb("#1A1410"),
            BarTextColor       = Color.FromArgb("#F0E6D3")
        };
    }
}
