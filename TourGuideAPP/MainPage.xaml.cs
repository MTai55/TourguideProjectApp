using Supabase;

namespace TourGuideAPP;

public partial class MainPage : ContentPage
{
    private readonly Client _supabase;

    public MainPage(Client supabase)
    {
        InitializeComponent();
        _supabase = supabase;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await TestConnection();
    }

    private async Task TestConnection()
{
    try
    {
        await _supabase.InitializeAsync();
        await DisplayAlertAsync("✅ Thành công", "Kết nối Supabase OK!", "OK");
    }
    catch (Exception ex)
    {
        await DisplayAlertAsync("❌ Lỗi", ex.Message, "OK");
    }
}
}