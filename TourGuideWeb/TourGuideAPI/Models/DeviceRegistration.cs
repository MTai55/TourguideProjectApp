using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TourGuideAPI.Models;

[Table("DeviceRegistrations")]
public class DeviceRegistration
{
    [Key]
    [Column("DeviceId")]
    public string DeviceId { get; set; } = string.Empty;

    [Column("Platform")]
    public string? Platform { get; set; }

    [Column("FirstSeenAt")]
    public DateTime? FirstSeenAt { get; set; }

    [Column("LastSeenAt")]
    public DateTime? LastSeenAt { get; set; }
}
