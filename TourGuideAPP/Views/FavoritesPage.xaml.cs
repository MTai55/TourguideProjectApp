using TourGuideAPP.Services;

namespace TourGuideAPP.Views;

public partial class FavoritesPage : ContentPage
{
    private readonly UserProfileService _profileService;

    public FavoritesPage(UserProfileService profileService)
    {
        InitializeComponent();
        _profileService = profileService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadFavorites();
    }

    private async Task LoadFavorites()
    {
        var list = await _profileService.GetFavoritesAsync();
        FavoritesCollection.ItemsSource = list;
        FavoritesEmptyLabel.IsVisible = !list.Any();
    }

    private async void OnRemoveClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is int placeId)
        {
            await _profileService.RemoveFavoriteAsync(placeId);
            await LoadFavorites();
        }
    }

    private async void OnClearAllClicked(object sender, EventArgs e)
    {
        var ok = await DisplayAlert("Xóa tất cả", "Bạn có chắc muốn xóa toàn bộ danh sách yêu thích?", "Có", "Không");
        if (!ok) return;

        var list = await _profileService.GetFavoritesAsync();
        foreach (var item in list)
            await _profileService.RemoveFavoriteAsync(item.PlaceId);

        await LoadFavorites();
    }
}
