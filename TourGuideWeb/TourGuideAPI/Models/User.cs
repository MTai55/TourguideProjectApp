namespace TourGuideAPI.Models;

public class User
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? PasswordHash { get; set; }
    public string Role { get; set; } = "User"; // User | Owner | Admin
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    // Navigation
    public ICollection<Place> OwnedPlaces { get; set; } = [];
    public ICollection<UserTracking> TrackingLogs { get; set; } = [];
    public ICollection<VisitHistory> VisitHistory { get; set; } = [];
    public ICollection<Review> Reviews { get; set; } = [];
}