namespace TourGuideAPI.Models;

public class Category
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? ColorHex { get; set; }
    public ICollection<Place> Places { get; set; } = [];
}