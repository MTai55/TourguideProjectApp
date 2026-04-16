using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TourGuideAPI.Models;

[Table("DevicePoiVisits")]
public class DevicePoiVisit
{
    [Key]
    [Column("VisitId")]
    public long VisitId { get; set; }

    [Column("DeviceId")]
    public string DeviceId { get; set; } = string.Empty;

    [Column("PlaceId")]
    public int PlaceId { get; set; }

    [Column("PlaceName")]
    public string? PlaceName { get; set; }

    [Column("VisitMethod")]
    public string VisitMethod { get; set; } = "GPS";

    [Column("VisitedAt")]
    public DateTime? VisitedAt { get; set; }
}
