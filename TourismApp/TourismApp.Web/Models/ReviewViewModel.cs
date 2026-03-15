public class ReviewViewModel
{
    public int ReviewId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public byte Rating { get; set; }
    public string? Comment { get; set; }
    public string? OwnerReply { get; set; }
    public DateTime CreatedAt { get; set; }
    public int PlaceId { get; set; }
    public string PlaceName { get; set; } = string.Empty;
}