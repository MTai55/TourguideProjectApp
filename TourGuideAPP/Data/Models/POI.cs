using Postgrest.Attributes;
using Postgrest.Models;

namespace TourGuideAPP.Data.Models;

[Table("pois")]
public class POI : BaseModel
{
    [PrimaryKey("id")]
    public string Id { get; set; } = string.Empty;

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string Description { get; set; } = string.Empty;

    [Column("latitude")]
    public double Latitude { get; set; }

    [Column("longitude")]
    public double Longitude { get; set; }

    [Column("radius")]
    public double Radius { get; set; } = 50;

    [Column("priority")]
    public int Priority { get; set; } = 1;

    [Column("tts_script")]
    public string TtsScript { get; set; } = string.Empty;

    [Column("audio_file_url")]
    public string AudioFileUrl { get; set; } = string.Empty;

    [Column("image_url")]
    public string ImageUrl { get; set; } = string.Empty;

    [Column("cooldown_minutes")]
    public int CooldownMinutes { get; set; } = 10;

    public DateTime? LastPlayedAt { get; set; }
}