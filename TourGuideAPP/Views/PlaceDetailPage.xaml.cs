using Microsoft.Maui.ApplicationModel.Communication;
using TourGuideAPP.Data.Models;
using TourGuideAPP.Resources.Strings;
using TourGuideAPP.Services;

namespace TourGuideAPP.Views;

public partial class PlaceDetailPage : ContentPage
{
    private readonly Place _place;
    private readonly AuthService _authService;
    private readonly LocationService _locationService;
    private readonly GeofenceEngine _geofenceEngine;
    private readonly NarrationService _narrationService;
    private readonly UserProfileService _userProfileService;
    private List<PlaceNote> _placeNotes = new();

    public PlaceDetailPage(
        Place place,
        AuthService authService,
        LocationService locationService,
        GeofenceEngine geofenceEngine,
        NarrationService narrationService,
        UserProfileService userProfileService)
    {
        InitializeComponent();
        _place = place;
        _authService = authService;
        _locationService = locationService;
        _geofenceEngine = geofenceEngine;
        _narrationService = narrationService;
        _userProfileService = userProfileService;

        LoadPlaceDetail();
    }

    private void LoadPlaceDetail()
    {
        MainImage.Source = _place.ImageUrl;
        NameLabel.Text = _place.Name;

        RatingLabel.Text = _place.AverageRating.HasValue
            ? $"★ {_place.AverageRating:F1}"
            : AppResources.PlaceNoRating;
        ReviewsLabel.Text = _place.TotalReviews.HasValue
            ? $"({_place.TotalReviews} {AppResources.PlaceReviewsSuffix})"
            : "";

        // Status badge
        bool isOpen = IsPlaceOpen(_place);
        StatusLabel.Text = isOpen ? AppResources.PlaceOpenNow : AppResources.PlaceClosedNow;
        StatusLabel.TextColor = isOpen ? Color.FromArgb("#4CAF50") : Color.FromArgb("#E94560");
        StatusBadge.BackgroundColor = isOpen ? Color.FromArgb("#1B3A28") : Color.FromArgb("#3A1B20");

        DescriptionLabel.Text = _place.Description ?? AppResources.PlaceNoDescription;
        AddressLabel.Text = _place.Address ?? AppResources.PlaceNoUpdate;
        OpenTimeLabel.Text = _place.OpenTime != null
            ? $"{_place.OpenTime} – {_place.CloseTime}"
            : AppResources.PlaceNoUpdate;
        PriceLabel.Text = _place.PriceMin.HasValue
            ? $"{_place.PriceMin:N0}đ – {_place.PriceMax:N0}đ"
            : AppResources.PlaceContactForPrice;

        if (!string.IsNullOrWhiteSpace(_place.Website))
            WebsiteLabel.Text = _place.Website;
        else
            WebsiteRow.IsVisible = false;

        // Load TTS Script
        if (!string.IsNullOrWhiteSpace(_place.TtsScript))
            TtsScriptLabel.Text = _place.TtsScript;
        else
            TtsScriptLabel.Text = "Chưa có cẩu chuyện hướng dẫn";
    }

    private static bool IsPlaceOpen(Place place)
    {
        if (place.OpenTime is null || place.CloseTime is null) return false;
        if (!TimeSpan.TryParse(place.OpenTime, out var open) ||
            !TimeSpan.TryParse(place.CloseTime, out var close)) return false;
        var now = DateTime.Now.TimeOfDay;
        return now >= open && now <= close;
    }

    private void OnBackClicked(object sender, TappedEventArgs e)
        => Navigation.PopAsync();

    private async void OnDirectionsClicked(object sender, TappedEventArgs e)
    {
        if (_place.Latitude == 0 && _place.Longitude == 0)
        {
            await DisplayAlert(AppResources.AlertError, AppResources.AlertNoCoords, AppResources.AlertOk);
            return;
        }
        MapPage.PendingRoute = (_place.Latitude, _place.Longitude, _place.Name);
        await Shell.Current.GoToAsync("//MainTabs/MapPage");
    }

    private async void OnGoogleMapsClicked(object sender, TappedEventArgs e)
    {
        // Deep link ưu tiên: tìm theo tên + tọa độ → mở Google Maps app
        // Nếu chưa cài Google Maps thì fallback sang trình duyệt
        var name = Uri.EscapeDataString(_place.Name ?? "");
        var lat = _place.Latitude;
        var lon = _place.Longitude;

        // geo URI → mở Google Maps app trực tiếp (Android)
        var geoUri = $"geo:{lat},{lon}?q={lat},{lon}({name})";

        // Fallback URL cho trình duyệt
        var webUrl = $"https://www.google.com/maps/search/?api=1&query={lat},{lon}";

        try
        {
            await Launcher.OpenAsync(new Uri(geoUri));
        }
        catch
        {
            // Thiết bị không có Google Maps → mở trình duyệt
            await Launcher.OpenAsync(new Uri(webUrl));
        }
    }

    private void OnCallClicked(object sender, TappedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_place.Phone))
        {
            DisplayAlert(AppResources.AlertInfo, AppResources.PlaceNoPhone, AppResources.AlertOk);
            return;
        }
        try { PhoneDialer.Open(_place.Phone); }
        catch { DisplayAlert(AppResources.AlertError, AppResources.AlertCannotCall, AppResources.AlertOk); }
    }

    private async void OnWebsiteClicked(object sender, TappedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_place.Website)) return;
        try { await Launcher.OpenAsync(new Uri(_place.Website)); }
        catch { await DisplayAlert(AppResources.AlertError, AppResources.AlertCannotWebsite, AppResources.AlertOk); }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Load ghi chú lịch sử của quán này
        var allNotes = await _userProfileService.GetNotesAsync();
        _placeNotes = allNotes.Where(n => n.PlaceId == _place.PlaceId).ToList();
        RefreshNotesDisplay();
    }

    private void RefreshNotesDisplay()
    {
        NotesHistoryStack.Children.Clear();
        if (_placeNotes.Count == 0)
        {
            var emptyLabel = new Label
            {
                Text = "Chưa có ghi chú nào",
                TextColor = Color.FromArgb("#5A4A3A"),
                FontSize = 12,
                HorizontalOptions = LayoutOptions.Center
            };
            NotesHistoryStack.Children.Add(emptyLabel);
        }
        else
        {
            // Sắp xếp theo ngày mới nhất trước
            foreach (var note in _placeNotes.OrderByDescending(n => n.CreatedAt))
            {
                var noteCard = CreateNoteCard(note);
                NotesHistoryStack.Children.Add(noteCard);
            }
        }
    }

    private Border CreateNoteCard(PlaceNote note)
    {
        var card = new Border
        {
            BackgroundColor = Color.FromArgb("#1A1410"),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(10) },
            Stroke = Color.FromArgb("#2A2018"),
            StrokeThickness = 1,
            Padding = new Thickness(12, 10),
            Margin = new Thickness(0, 0, 0, 8)
        };

        var grid = new Grid { ColumnDefinitions = "*,Auto", ColumnSpacing = 8 };

        var contentStack = new VerticalStackLayout { Spacing = 2 };
        contentStack.Children.Add(new Label
        {
            Text = note.Content,
            TextColor = Color.FromArgb("#F0E6D3"),
            FontSize = 13,
            LineBreakMode = LineBreakMode.WordWrap
        });
        contentStack.Children.Add(new Label
        {
            Text = note.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
            TextColor = Color.FromArgb("#8A7560"),
            FontSize = 10
        });

        grid.Add(contentStack, 0);

        var deleteBtn = new Label
        {
            Text = "✕",
            TextColor = Color.FromArgb("#E94560"),
            FontSize = 18,
            VerticalOptions = LayoutOptions.Start
        };
        deleteBtn.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () => await OnDeleteNoteClicked(note))
        });
        grid.Add(deleteBtn, 1);
        Grid.SetColumn(deleteBtn, 1);

        card.Content = grid;
        return card;
    }

    private async Task OnDeleteNoteClicked(PlaceNote note)
    {
        var confirm = await DisplayAlert("Xóa ghi chú", "Bạn chắc muốn xóa ghi chú này?", "Xóa", "Hủy");
        if (confirm)
        {
            await _userProfileService.RemoveNoteAsync(note);
            _placeNotes.Remove(note);
            RefreshNotesDisplay();
        }
    }

    private async void OnAddNoteClicked(object sender, TappedEventArgs e)
    {
        var content = NoteInputEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(content))
        {
            await DisplayAlert("Lỗi", "Vui lòng nhập ghi chú trước.", "OK");
            return;
        }

        await _userProfileService.AddNoteAsync(_place.PlaceId, _place.Name, content);
        _placeNotes.Add(new PlaceNote
        {
            PlaceId = _place.PlaceId,
            Name = _place.Name,
            Content = content,
            CreatedAt = DateTime.Now
        });
        NoteInputEntry.Text = string.Empty;
        RefreshNotesDisplay();
        await DisplayAlert("Thành công", "Ghi chú đã lưu.", "OK");
    }

    private async void OnCopyTtsClicked(object sender, TappedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_place.TtsScript))
        {
            await DisplayAlert("Thông báo", "Quán này chưa có script TTS.", "OK");
            return;
        }
        await Clipboard.SetTextAsync(_place.TtsScript);
        await DisplayAlert("Thành công", "Script TTS đã sao chép.", "OK");
    }

    private async void OnPlayTtsClicked(object sender, TappedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_place.TtsScript))
        {
            await DisplayAlert("Thông báo", "Quán này chưa có script TTS.", "OK");
            return;
        }
        await _narrationService.SpeakAsync(_place.GetScriptForLocale(_narrationService.PreferredLocale));
    }
}
