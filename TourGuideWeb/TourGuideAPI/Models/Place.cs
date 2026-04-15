namespace TourGuideAPI.Models;

public class Place
{
    public int PlaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Phone { get; set; }
    public string? Website { get; set; }
    public TimeOnly? OpenTime { get; set; }
    public TimeOnly? CloseTime { get; set; }
    public decimal? PriceMin { get; set; }
    public decimal? PriceMax { get; set; }
    public string? Specialty { get; set; }
    public int? PricePerPerson { get; set; }
    public string? District { get; set; }
    public bool HasParking { get; set; }
    public bool HasAircon { get; set; }
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public int TotalVisits { get; set; }
    public string Status { get; set; } = "Pending";
    public string OpenStatus { get; set; } = "Closed";
    public bool IsActive { get; set; } = true;
    public bool IsApproved => Status == "Active";
    public int? CategoryId { get; set; }
    public int OwnerId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? tts_script { get; set; }
    public string? tts_translations { get; set; } 
    public double? radius { get; set; } = 100;
    public User? Owner { get; set; }
    public Category? Category { get; set; }
    public ICollection<PlaceImage> Images { get; set; } = [];
    public ICollection<Review> Reviews { get; set; } = [];
    public ICollection<VisitHistory> VisitHistory { get; set; } = [];
    public ICollection<Message> Messages { get; set; } = [];
    public ICollection<Promotion> Promotions { get; set; } = [];
}

public class PlaceImage
{
    public int ImageId { get; set; }
    public int PlaceId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsMain { get; set; }
    public int SortOrder { get; set; }
    public Place? Place { get; set; }
}