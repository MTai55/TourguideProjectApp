using System.Collections.Generic;

namespace TourGuideAPI.Models;

public static class ToursDataStore
{
    private static readonly List<TourData> _tours = new()
    {
        new TourData
        {
            Id = "tour-1",
            Title = "Foodie Khánh Hội",
            Description = "Đi bộ nhẹ nhàng, ăn ngon, nhiều điểm check-in.",
            DurationText = "2–3 giờ",
            BudgetText = "100k–250k",
            Tag = "Ăn vặt",
            StopPlaceIds = new List<int> { 1, 2, 3 }
        },
        new TourData
        {
            Id = "tour-2",
            Title = "Cafe & Chill",
            Description = "Ưu tiên quán đẹp – yên tĩnh, ít di chuyển.",
            DurationText = "2 giờ",
            BudgetText = "80k–200k",
            Tag = "Cafe",
            StopPlaceIds = new List<int> { 4, 5 }
        }
    };

    public static IEnumerable<TourData> GetAllTours() => _tours;

    public static TourData? GetTourById(string id) => _tours.FirstOrDefault(t => t.Id == id);

    public static void AddTour(TourData tour) => _tours.Add(tour);

    public static bool UpdateTour(string id, TourData updatedTour)
    {
        var existing = _tours.FirstOrDefault(t => t.Id == id);
        if (existing == null) return false;

        existing.Title = updatedTour.Title;
        existing.Description = updatedTour.Description;
        existing.DurationText = updatedTour.DurationText;
        existing.BudgetText = updatedTour.BudgetText;
        existing.Tag = updatedTour.Tag;
        existing.StopPlaceIds = updatedTour.StopPlaceIds;

        return true;
    }

    public static bool DeleteTour(string id)
    {
        var tour = _tours.FirstOrDefault(t => t.Id == id);
        if (tour == null) return false;

        _tours.Remove(tour);
        return true;
    }
}

public class TourData
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DurationText { get; set; } = string.Empty;
    public string BudgetText { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public List<int> StopPlaceIds { get; set; } = new();
}