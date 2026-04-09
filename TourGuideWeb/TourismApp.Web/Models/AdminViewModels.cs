namespace TourismApp.Web.Models;

public class AdminStatsViewModel
{
    public int TotalUsers { get; set; }
    public int TotalPlaces { get; set; }
    public int PendingPlaces { get; set; }
    public int TotalReviews { get; set; }
    public int HiddenReviews { get; set; }
    public double AvgRating { get; set; }
}

public class UserViewModel
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
