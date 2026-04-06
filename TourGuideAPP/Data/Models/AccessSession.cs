using Postgrest.Attributes;
using Postgrest.Models;

namespace TourGuideAPP.Data.Models;

[Table("AccessSessions")]
public class AccessSession : BaseModel
{
    [PrimaryKey("SessionId", false)]
    public string SessionId { get; set; } = string.Empty;

    [Column("DeviceId")]
    public string DeviceId { get; set; } = string.Empty;

    [Column("PackageId")]
    public string PackageId { get; set; } = string.Empty;

    [Column("DurationHours")]
    public double DurationHours { get; set; }

    [Column("PriceVnd")]
    public int PriceVnd { get; set; }

    [Column("CreatedAt")]
    public DateTime? CreatedAt { get; set; }

    [Column("ActivatedAt")]
    public DateTime? ActivatedAt { get; set; }

    [Column("ExpiresAt")]
    public DateTime? ExpiresAt { get; set; }

    [Column("IsActive")]
    public bool IsActive { get; set; }
}
