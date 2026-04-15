using TourGuideAPP.Resources.Strings;
using TourGuideAPP.Services;

namespace TourGuideAPP.Views;

public partial class AccountPage : ContentPage
{
    private NarrationService? _narrationService;
    private AccessSessionService? _accessService;
    private UserProfileService? _userProfileService;

    public AccountPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _narrationService    = Handler?.MauiContext?.Services?.GetService<NarrationService>();
        _accessService       = Handler?.MauiContext?.Services?.GetService<AccessSessionService>();
        _userProfileService  = Handler?.MauiContext?.Services?.GetService<UserProfileService>();

        await LoadHistoryAsync();

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

    private async Task LoadHistoryAsync()
    {
        if (_userProfileService is null) return;

        var history = await _userProfileService.GetTripHistoryAsync();

        HistoryContainer.Children.Clear();

        if (history.Count == 0)
        {
            HistoryEmptyLabel.IsVisible = true;
            HistoryContainer.IsVisible  = false;
            HistoryClearBtn.IsVisible   = false;
            return;
        }

        HistoryEmptyLabel.IsVisible = false;
        HistoryContainer.IsVisible  = true;
        HistoryClearBtn.IsVisible   = true;

        // Hiện tối đa 30 mục gần nhất
        foreach (var (item, index) in history.Take(30).Select((x, i) => (x, i)))
        {
            // Separator (trừ phần tử đầu tiên)
            if (index > 0)
                HistoryContainer.Children.Add(new BoxView
                {
                    BackgroundColor = Color.FromArgb("#2A2018"),
                    HeightRequest = 1,
                    Margin = new Thickness(16, 0)
                });

            var icon = item.VisitMethod switch
            {
                "QR Code"  => "📷",
                "Booking"  => "🎫",
                _          => "📍"   // GPS / Manual / Unknown
            };

            var dateText = item.VisitedAt.ToString("dd/MM/yyyy HH:mm");

            var row = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection(
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Star)),
                ColumnSpacing = 12,
                Padding = new Thickness(16, 12)
            };

            // Icon badge
            var iconBadge = new Border
            {
                BackgroundColor = Color.FromArgb("#26201A"),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(10) },
                Stroke = Colors.Transparent,
                WidthRequest = 36, HeightRequest = 36,
                VerticalOptions = LayoutOptions.Center
            };
            iconBadge.Content = new Label
            {
                Text = icon, FontSize = 16,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };
            Grid.SetColumn(iconBadge, 0);

            // Text stack
            var textStack = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
            textStack.Children.Add(new Label
            {
                Text = item.Name,
                FontSize = 13, FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#F0E6D3"),
                LineBreakMode = LineBreakMode.TailTruncation
            });
            if (!string.IsNullOrWhiteSpace(item.Address))
                textStack.Children.Add(new Label
                {
                    Text = item.Address,
                    FontSize = 11, TextColor = Color.FromArgb("#5A4A3A"),
                    LineBreakMode = LineBreakMode.TailTruncation
                });
            textStack.Children.Add(new Label
            {
                Text = $"{dateText}  ·  {item.VisitMethod}",
                FontSize = 10, TextColor = Color.FromArgb("#3A2D22")
            });
            Grid.SetColumn(textStack, 1);

            row.Children.Add(iconBadge);
            row.Children.Add(textStack);
            HistoryContainer.Children.Add(row);
        }
    }

    private async void OnClearHistoryTapped(object sender, TappedEventArgs e)
    {
        if (_userProfileService is null) return;

        bool confirmed = await DisplayAlert(
            AppResources.AccHistoryClearTitle,
            AppResources.AccHistoryClearMsg,
            AppResources.AccHistoryClearYes,
            AppResources.AlertOk);

        if (!confirmed) return;

        await _userProfileService.ClearHistoryAsync();
        await LoadHistoryAsync();
    }

    // DEV ONLY: huỷ kích hoạt để test lại luồng thanh toán
    private void OnDevDeactivateTapped(object sender, TappedEventArgs e)
    {
        _accessService?.ClearLocalSession();
        var langPage = Handler!.MauiContext!.Services.GetRequiredService<LanguageSelectionPage>();
        Application.Current!.MainPage = new NavigationPage(langPage)
        {
            BarBackgroundColor = Color.FromArgb("#1A1410"),
            BarTextColor       = Color.FromArgb("#F0E6D3")
        };
    }
}
