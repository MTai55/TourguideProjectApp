using TourGuideAPP.Services;

namespace TourGuideAPP.Views;

public partial class WishlistPage : ContentPage
{
    private readonly UserProfileService _profileService;

    public WishlistPage(UserProfileService profileService)
    {
        InitializeComponent();
        _profileService = profileService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadWishlist();
    }

    private async Task LoadWishlist()
    {
        WishlistCollection.ItemsSource = await _profileService.GetWishlistAsync();
    }

    private async void OnRemoveClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is int placeId)
        {
            await _profileService.RemoveWishlistAsync(placeId);
            await LoadWishlist();
        }
    }

    private async void OnClearAllClicked(object sender, EventArgs e)
    {
        var ok = await DisplayAlert("Xóa tất cả", "Bạn có chắc muốn xóa toàn bộ danh sách muốn đi?", "Có", "Không");
        if (!ok) return;

        var list = await _profileService.GetWishlistAsync();
        foreach (var item in list)
            await _profileService.RemoveWishlistAsync(item.PlaceId);

        await LoadWishlist();
    }
}
