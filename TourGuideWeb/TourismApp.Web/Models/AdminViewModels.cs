namespace TourismApp.Web.Models;

public class AdminStatsViewModel
{
    public int TotalUsers { get; set; }
    public int TotalPlaces { get; set; }
    public int PendingPlaces { get; set; }
    public int TotalReviews { get; set; }
    public int PendingComplaints { get; set; }
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

public class ComplaintViewModel
{
    public int ComplaintId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? AdminNote { get; set; }
    public DateTime CreatedAt { get; set; }
}