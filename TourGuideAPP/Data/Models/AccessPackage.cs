using Postgrest.Attributes;
using Postgrest.Models;

namespace TourGuideAPP.Data.Models;

[Table("AccessPackages")]
public class AccessPackage : BaseModel
{
    [PrimaryKey("PackageId", false)]
    public string PackageId { get; set; } = string.Empty;

    [Column("DurationHours")]
    public double DurationHours { get; set; }

    [Column("PriceVnd")]
    public int PriceVnd { get; set; }

    [Column("IsActive")]
    public bool IsActive { get; set; }

    [Column("SortOrder")]
    public int SortOrder { get; set; }
}
