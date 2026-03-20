using TourGuideAPP.Data.Models;
using TourGuideAPP.Services;

namespace TourGuideAPP.Views;

public partial class NotesPage : ContentPage
{
    private readonly UserProfileService _profileService;

    public NotesPage(UserProfileService profileService)
    {
        InitializeComponent();
        _profileService = profileService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadNotes();
    }

    private async Task LoadNotes()
    {
        NotesCollection.ItemsSource = await _profileService.GetNotesAsync();
    }

    private async void OnAddNoteClicked(object sender, EventArgs e)
    {
        if (!int.TryParse(PlaceIdEntry.Text, out var placeId))
        {
            await DisplayAlert("Lỗi", "Place ID phải là số", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(PlaceNameEntry.Text) || string.IsNullOrWhiteSpace(NoteEditor.Text))
        {
            await DisplayAlert("Lỗi", "Vui lòng điền đầy đủ tên địa điểm và nội dung ghi chú", "OK");
            return;
        }

        await _profileService.AddNoteAsync(placeId, PlaceNameEntry.Text.Trim(), NoteEditor.Text.Trim());
        PlaceIdEntry.Text = string.Empty;
        PlaceNameEntry.Text = string.Empty;
        NoteEditor.Text = string.Empty;
        await LoadNotes();
    }

    private async void OnRemoveNoteClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is int placeId)
        {
            var notes = await _profileService.GetNotesAsync();
            var toRemove = notes.FirstOrDefault(x => x.PlaceId == placeId);
            if (toRemove != null)
            {
                await _profileService.RemoveNoteAsync(toRemove);
                await LoadNotes();
            }
        }
    }
}
