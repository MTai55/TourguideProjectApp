using System.Collections.ObjectModel;

namespace TourGuideAPP.Views;

public partial class ToursPage : ContentPage
{
    public ObservableCollection<TourCard> Tours { get; } = new()
    {
        new TourCard("foodie-1", "Foodie Khánh Hội", "Đi bộ nhẹ nhàng, ăn ngon, nhiều điểm check-in.", "2-3 giờ", "100k–250k", "Ăn vặt"),
        new TourCard("coffee-1", "Cafe & Chill", "Lộ trình ít di chuyển, ưu tiên quán đẹp – yên tĩnh.", "2 giờ", "80k–200k", "Cafe"),
        new TourCard("seafood-1", "Hải sản tối", "Đi theo nhóm, ưu tiên no – rẻ – nhanh.", "3-4 giờ", "200k–400k", "Tối"),
    };

    public ToursPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    private async void OnFilterClicked(object sender, EventArgs e)
    {
        await DisplayAlertAsync("Bộ lọc", "Sắp làm: lọc theo tiêu chí (món, ngân sách, thời gian, khoảng cách).", "OK");
    }

    private async void OnSelectTourClicked(object sender, EventArgs e)
    {
        if (sender is not Button btn || btn.CommandParameter is not string id)
            return;

        var selected = Tours.FirstOrDefault(t => t.Id == id);
        await DisplayAlertAsync("Chọn tour", selected is null ? "Không tìm thấy tour." : $"Bạn đã chọn: {selected.Title}", "OK");
    }

    public sealed record TourCard(
        string Id,
        string Title,
        string Description,
        string DurationText,
        string BudgetText,
        string Tag);
}

