using TourGuideAPP.Services;

namespace TourGuideAPP.Views;

public partial class TripHistoryPage : ContentPage
{
    private readonly UserProfileService _profileService;

    public TripHistoryPage(UserProfileService profileService)
    {
        InitializeComponent();
        _profileService = profileService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadHistory();
    }

    private async Task LoadHistory()
    {
        HistoryCollection.ItemsSource = await _profileService.GetTripHistoryAsync();
    }

    private async void OnClearAllClicked(object sender, EventArgs e)
    {
        var ok = await DisplayAlert("Xóa tất cả", "Bạn có chắc muốn xóa toàn bộ lịch sử hành trình?", "Có", "Không");
        if (!ok) return;

        await _profileService.ClearHistoryAsync();
        await LoadHistory();
    }
}
