namespace TourGuideAPI.Models;

public class Complaint
{
    public int ComplaintId { get; set; }
    public int UserId { get; set; }
    public int? PlaceId { get; set; }
    public int? ReviewId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string? AdminReply { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
    public User? User { get; set; }
    public Place? Place { get; set; }
}