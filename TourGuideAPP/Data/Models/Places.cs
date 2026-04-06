using Postgrest.Attributes;
using Postgrest.Models;

namespace TourGuideAPP.Data.Models;

[Table("Places")]
public class Place : BaseModel
{
    [PrimaryKey("PlaceId")]
    public int PlaceId { get; set; }

    [Column("Name")]
    public string Name { get; set; } = string.Empty;

    [Column("Description")]
    public string? Description { get; set; }

    [Column("Address")]
    public string? Address { get; set; }

    [Column("Latitude")]
    public double Latitude { get; set; }

    [Column("Longitude")]
    public double Longitude { get; set; }

    [Column("Phone")]
    public string? Phone { get; set; }

    [Column("Website")]
    public string? Website { get; set; }

    [Column("OpenTime")]
    public string? OpenTime { get; set; }

    [Column("CloseTime")]
    public string? CloseTime { get; set; }

    [Column("PriceMin")]
    public decimal? PriceMin { get; set; }

    [Column("PriceMax")]
    public decimal? PriceMax { get; set; }

    [Column("AverageRating")]
    public float? AverageRating { get; set; }

    [Column("TotalReviews")]
    public int? TotalReviews { get; set; }

    [Column("IsApproved")]
    public bool IsApproved { get; set; }

    [Column("IsActive")]
    public bool IsActive { get; set; }

    [Column("CategoryId")]
    public int? CategoryId { get; set; }

    [Column("OwnerId")]
    public int? OwnerId { get; set; }

    [Column("Specialty")]
    public string? Specialty { get; set; }

    [Column("PricePerPerson")]
    public int? PricePerPerson { get; set; }

    [Column("District")]
    public string? District { get; set; }

    [Column("HasParking")]
    public bool? HasParking { get; set; }

    [Column("HasAircon")]
    public bool? HasAircon { get; set; }

    // ── Cột mới thêm vào DB ── đều nullable vì data cũ chưa có giá trị
    [Column("tts_script")]
    public string? TtsScript { get; set; }

    [Column("audio_file_url")]
    public string? AudioFileUrl { get; set; }

    [Column("radius")]
    public double? Radius { get; set; }

    [Column("cooldown_minutes")]
    public int? CooldownMinutes { get; set; }

    [Column("priority")]
    public int? Priority { get; set; }

    // Không map DB — chỉ dùng trong runtime
    public DateTime? LastPlayedAt { get; set; }

    // Locale TTS riêng cho địa điểm này, vd "en-US", "vi-VN"
    // Chưa có [Column] — cần thêm cột "tts_locale" trong Supabase trước
    public string? TtsLocale { get; set; }

    // ── UI helpers — không map DB ──
    public string ImageUrl { get; set; } = "https://via.placeholder.com/400x200";
    public string OpenTimeDisplay => OpenTime != null ? $"{OpenTime} - {CloseTime}" : "Chưa cập nhật";
    public string RatingDisplay   => AverageRating.HasValue ? $"⭐ {AverageRating:F1}" : "Chưa có đánh giá";
    public string PriceDisplay    => PriceMin.HasValue ? $"{PriceMin:N0}đ - {PriceMax:N0}đ" : "Liên hệ";
}