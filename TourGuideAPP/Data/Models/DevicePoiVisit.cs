using Postgrest.Attributes;
using Postgrest.Models;

namespace TourGuideAPP.Data.Models;

[Table("DevicePoiVisits")]
public class DevicePoiVisit : BaseModel
{
    [PrimaryKey("VisitId", false)]
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
