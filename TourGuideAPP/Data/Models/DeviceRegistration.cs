using Postgrest.Attributes;
using Postgrest.Models;

namespace TourGuideAPP.Data.Models;

[Table("DeviceRegistrations")]
public class DeviceRegistration : BaseModel
{
    [PrimaryKey("DeviceId", false)]
    public string DeviceId { get; set; } = string.Empty;

    [Column("Platform")]
    public string? Platform { get; set; }

    [Column("FirstSeenAt")]
    public DateTime? FirstSeenAt { get; set; }

    [Column("LastSeenAt")]
    public DateTime? LastSeenAt { get; set; }
}
