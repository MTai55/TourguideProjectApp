namespace TourGuideAPI.DTOs.Subscriptions;

public record CreateSubscriptionDto(
    int    PlanId,
    string PaymentMethod  // vnpay | momo | stripe
);

public record PaymentCallbackDto(
    string TransactionId,
    string Status,        // success | failed
    int    Amount,
    string? Notes
);

public class SubscriptionPlanDto
{
    public int     PlanId       { get; set; }
    public string  Name         { get; set; } = string.Empty;
    public string  Slug         { get; set; } = string.Empty;
    public int     Price        { get; set; }
    public int     MaxPlaces    { get; set; }
    public bool    HasTts       { get; set; }
    public bool    HasAnalytics { get; set; }
    public bool    HasPriority  { get; set; }
    public List<string> Features { get; set; } = [];
}

public class SubscriptionDto
{
    public int     SubId         { get; set; }
    public string  PlanName      { get; set; } = string.Empty;
    public string  PlanSlug      { get; set; } = string.Empty;
    public string  Status        { get; set; } = string.Empty;
    public DateTime? StartDate   { get; set; }
    public DateTime? EndDate     { get; set; }
    public string? PaymentMethod { get; set; }
    public int     Amount        { get; set; }
    public bool    IsActive      { get; set; }
    public int?    DaysRemaining { get; set; }
}
