namespace TourGuideAPI.Models;

public class SubscriptionPlan
{
    public int     PlanId       { get; set; }
    public string  Name         { get; set; } = string.Empty;
    public string  Slug         { get; set; } = string.Empty;
    public int     Price        { get; set; }
    public int     MaxPlaces    { get; set; }
    public bool    HasTts       { get; set; }
    public bool    HasAnalytics { get; set; }
    public bool    HasPriority  { get; set; }
    public string? Features     { get; set; }
    public bool    IsActive     { get; set; } = true;
    public DateTime CreatedAt   { get; set; } = DateTime.UtcNow;

    public ICollection<Subscription> Subscriptions { get; set; } = [];
}

public class Subscription
{
    public int     SubId         { get; set; }
    public int     OwnerId       { get; set; }
    public int     PlanId        { get; set; }
    public string  Status        { get; set; } = "Pending";
    public DateTime? StartDate   { get; set; }
    public DateTime? EndDate     { get; set; }
    public string? PaymentMethod { get; set; }
    public string? TransactionId { get; set; }
    public int     Amount        { get; set; }
    public string? Notes         { get; set; }
    public DateTime CreatedAt    { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt   { get; set; }

    // Navigation
    public User?             Owner { get; set; }
    public SubscriptionPlan? Plan  { get; set; }

    // Computed
    public bool IsActive  => Status == "Active" && EndDate > DateTime.UtcNow;
    public bool IsExpired => Status == "Active" && EndDate <= DateTime.UtcNow;
}
