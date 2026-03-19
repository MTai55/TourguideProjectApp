namespace TourGuideAPI.Models;

public class VisitHistory
{
    public int VisitId { get; set; }
    public int UserId { get; set; }
    public int PlaceId { get; set; }
    public DateTime CheckInTime { get; set; } = DateTime.UtcNow;
    public DateTime? CheckOutTime { get; set; }
    public int? DurationMins { get; set; }
    public bool AutoDetected { get; set; }
    public string? Notes { get; set; }
    public User? User { get; set; }
    public Place? Place { get; set; }
}