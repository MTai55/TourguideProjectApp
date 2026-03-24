namespace TourGuideAPI.Models;

public class Promotion
{
    public int PromoId { get; set; }
    public int PlaceId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? Discount { get; set; }
    public string? VoucherCode { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsExpired => DateTime.UtcNow > EndDate;
    public Place? Place { get; set; }
}