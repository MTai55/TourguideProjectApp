using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TourGuideAPI.Models;

[Table("AccessSessions")]
public class AccessSession
{
    [Key]
    [Column("SessionId")]
    public Guid SessionId { get; set; }

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
