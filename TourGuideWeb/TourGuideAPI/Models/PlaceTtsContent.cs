namespace TourGuideAPI.Models;

public class PlaceTtsContent
{
    public int Id { get; set; }
    public int PlaceId { get; set; }
    public string Locale { get; set; } = string.Empty;
    public string Script { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public Place? Place { get; set; }
}
