using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace TourismApp.Web.Models;

public class PlaceViewModel
{
    public int PlaceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Phone { get; set; }
    public string? OpenTime { get; set; }
    public string? CloseTime { get; set; }
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public int TotalVisits { get; set; }
    public string? CategoryName { get; set; }
    public string? MainImageUrl { get; set; }
    public bool IsApproved { get; set; }
    public string? Specialty { get; set; }
    public int? PricePerPerson { get; set; }
    public decimal? PriceMin { get; set; }
    public decimal? PriceMax { get; set; }
    public string? District { get; set; }
    public bool HasParking { get; set; }
    public bool HasAircon { get; set; }
    public string Status { get; set; } = "Pending";
    public string OpenStatus { get; set; } = "Closed";
    public string? OwnerName { get; set; }
    [JsonProperty("categoryId")]
        
    public int? CategoryId { get; set; }

    [JsonProperty("ttsScript")]
    public string? TtsScript { get; set; }

    [JsonProperty("ttsTranslations")]
    public string? TtsTranslations { get; set; }

    [JsonProperty("radius")]
    public double? Radius { get; set; }

}

public class CreatePlaceViewModel
{
    [Required(ErrorMessage = "Tên quán không được để trống")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required(ErrorMessage = "Địa chỉ không được để trống")]
    [MaxLength(500)]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vĩ độ không được để trống")]
    public double Latitude { get; set; }

    [Required(ErrorMessage = "Kinh độ không được để trống")]
    public double Longitude { get; set; }

    public string? Phone { get; set; }
    public string? OpenTime { get; set; }
    public string? CloseTime { get; set; }
    public decimal? PriceMin { get; set; }
    public decimal? PriceMax { get; set; }
    public int? CategoryId { get; set; }
    public string? Specialty { get; set; }
    public int? PricePerPerson { get; set; }
    public string? District { get; set; }
    public bool HasParking { get; set; }
    public bool HasAircon { get; set; }

    // ── Thông tin cũ (để tham khảo khi chỉnh sửa) ──
    [JsonIgnore]
    public string? PreviousOpenTime { get; set; }
    [JsonIgnore]
    public string? PreviousCloseTime { get; set; }
    [JsonIgnore]
    public decimal? PreviousPriceMin { get; set; }
    [JsonIgnore]
    public decimal? PreviousPriceMax { get; set; }
    [JsonIgnore]
    public string? PreviousTtsScript { get; set; }
    [JsonIgnore]
    public DateTime? LastModifiedAt { get; set; }
}

public class ReviewViewModel
{
    public int ReviewId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? UserAvatar { get; set; }
    public byte Rating { get; set; }
    public string? Comment { get; set; }
    public string? OwnerReply { get; set; }
    public DateTime CreatedAt { get; set; }
    public int PlaceId { get; set; }
    public string PlaceName { get; set; } = string.Empty;
    public byte? TasteRating { get; set; }
    public byte? PriceRating { get; set; }
    public byte? SpaceRating { get; set; }
    public bool IsHidden { get; set; }
    public string? HiddenNote { get; set; }
}

public class PromotionViewModel
{
    public int PromoId { get; set; }
    public int PlaceId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? Discount { get; set; }
    public string? VoucherCode { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public bool IsExpired => DateTime.UtcNow > EndDate;
}

public class CreatePromotionViewModel
{
    public int PlaceId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tiêu đề")]
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    [Range(1, 100, ErrorMessage = "% giảm từ 1-100")]
    public int? Discount { get; set; }
    public string? VoucherCode { get; set; }

    [Required]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [Required]
    public DateTime EndDate { get; set; } = DateTime.Today.AddDays(7);
}

public class PlaceImageViewModel
{
    public int ImageId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsMain { get; set; }
}

public class SubscriptionPlanViewModel
{
    [Newtonsoft.Json.JsonProperty("planId")]
    public int    PlanId       { get; set; }
    [Newtonsoft.Json.JsonProperty("name")]
    public string Name         { get; set; } = string.Empty;
    [Newtonsoft.Json.JsonProperty("slug")]
    public string Slug         { get; set; } = string.Empty;
    [Newtonsoft.Json.JsonProperty("price")]
    public int    Price        { get; set; }
    [Newtonsoft.Json.JsonProperty("maxPlaces")]
    public int    MaxPlaces    { get; set; }
    [Newtonsoft.Json.JsonProperty("hasTts")]
    public bool   HasTts       { get; set; }
    [Newtonsoft.Json.JsonProperty("hasAnalytics")]
    public bool   HasAnalytics { get; set; }
    [Newtonsoft.Json.JsonProperty("hasPriority")]
    public bool   HasPriority  { get; set; }
    [Newtonsoft.Json.JsonProperty("features")]
    public List<string> Features { get; set; } = [];
}

public class SubscriptionDto
{
    [Newtonsoft.Json.JsonProperty("subId")]
    public int     SubId         { get; set; }
    [Newtonsoft.Json.JsonProperty("planName")]
    public string  PlanName      { get; set; } = string.Empty;
    [Newtonsoft.Json.JsonProperty("planSlug")]
    public string  PlanSlug      { get; set; } = string.Empty;
    [Newtonsoft.Json.JsonProperty("status")]
    public string  Status        { get; set; } = string.Empty;
    [Newtonsoft.Json.JsonProperty("startDate")]
    public DateTime? StartDate   { get; set; }
    [Newtonsoft.Json.JsonProperty("endDate")]
    public DateTime? EndDate     { get; set; }
    [Newtonsoft.Json.JsonProperty("paymentMethod")]
    public string? PaymentMethod { get; set; }
    [Newtonsoft.Json.JsonProperty("amount")]
    public int     Amount        { get; set; }
    [Newtonsoft.Json.JsonProperty("isActive")]
    public bool    IsActive      { get; set; }
    [Newtonsoft.Json.JsonProperty("daysRemaining")]
    public int?    DaysRemaining { get; set; }
}