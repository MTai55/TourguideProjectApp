namespace TourGuideAPI.Models;

public class UserTracking
{
    public long TrackId { get; set; }
    public int UserId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Accuracy { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    public User? User { get; set; }
}