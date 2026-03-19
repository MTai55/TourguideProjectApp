namespace TourGuideAPI.Models;

public class Complaint
{
    public int ComplaintId { get; set; }
    public int UserId { get; set; }
    public int? PlaceId { get; set; }
    public int? ReviewId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string? AdminNote { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
    public User? User { get; set; }
}