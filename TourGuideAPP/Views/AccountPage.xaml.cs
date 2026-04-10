using TourGuideAPP.Services;

namespace TourGuideAPP.Views;

public partial class AccountPage : ContentPage
{
    private NarrationService? _narrationService;
    private AccessSessionService? _accessService;

    public AccountPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _narrationService = Handler?.MauiContext?.Services?.GetService<NarrationService>();
        _accessService    = Handler?.MauiContext?.Services?.GetService<AccessSessionService>();

        // Populate TTS locale picker
        TtsLocalePicker.ItemsSource = NarrationService.SupportedLocales
            .Select(l => l.Display)
            .ToList();

        var currentLocale = _narrationService?.PreferredLocale ?? "vi-VN";
        var idx = NarrationService.SupportedLocales
            .ToList()
            .FindIndex(l => l.Locale == currentLocale);
        TtsLocalePicker.SelectedIndex = idx >= 0 ? idx : 0;

        TtsLocaleSubtitle.Text = idx >= 0
            ? NarrationService.SupportedLocales[idx].Display
            : NarrationService.SupportedLocales[0].Display;
    }

    private void OnTtsLocaleChanged(object sender, EventArgs e)
    {
        if (_narrationService is null) return;
        var idx = TtsLocalePicker.SelectedIndex;
        if (idx < 0 || idx >= NarrationService.SupportedLocales.Count) return;

        var selected = NarrationService.SupportedLocales[idx];
        _narrationService.PreferredLocale = selected.Locale;
        TtsLocaleSubtitle.Text = selected.Display;
    }

    // DEV ONLY: huỷ kích hoạt để test lại luồng thanh toán
    private void OnDevDeactivateTapped(object sender, TappedEventArgs e)
    {
        _accessService?.ClearLocalSession();
        Application.Current!.MainPage = new NavigationPage(new SubscriptionPage(
            Handler!.MauiContext!.Services.GetRequiredService<AccessSessionService>()));
    }
}
