using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TourGuideAPI.Models;

[Table("AccessPackages")]
public class AccessPackage
{
    [Key]
    [Column("PackageId")]
    public string PackageId { get; set; } = string.Empty;

    [Column("DurationHours")]
    public double DurationHours { get; set; }

    [Column("PriceVnd")]
    public int PriceVnd { get; set; }

    [Column("IsActive")]
    public bool IsActive { get; set; } = true;

    [Column("SortOrder")]
    public int SortOrder { get; set; }
}
