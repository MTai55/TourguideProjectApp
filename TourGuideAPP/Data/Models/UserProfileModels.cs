namespace TourGuideAPP.Data.Models;

public class TripHistoryItem
{
    public int PlaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime VisitedAt { get; set; }
    public string VisitMethod { get; set; } = "Unknown";
}

public class PlaceNote
{
    public int PlaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}