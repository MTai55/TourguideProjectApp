namespace TourGuideAPI.Models;

public class Staff
{
    public int StaffId { get; set; }
    public int PlaceId { get; set; }
    public int? UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Role { get; set; } = "Staff";
    public bool IsActive { get; set; } = true;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public Place? Place { get; set; }
}