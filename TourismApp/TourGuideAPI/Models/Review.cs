namespace TourGuideAPI.Models;

public class Review
{
    public int ReviewId { get; set; }
    public int UserId { get; set; }
    public int PlaceId { get; set; }
    public byte Rating { get; set; }  // 1-5
    public string? Comment { get; set; }
    public byte? TasteRating { get; set; }
    public byte? PriceRating { get; set; }
    public byte? SpaceRating { get; set; }
    public string? OwnerReply { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public User? User { get; set; }
    public Place? Place { get; set; }
}