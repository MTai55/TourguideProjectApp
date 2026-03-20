using Postgrest.Attributes;
using Postgrest.Models;

namespace TourGuideAPP.Data.Models;

[Table("Users")]
public class User : BaseModel
{
    [PrimaryKey("UserId")]
    public int UserId { get; set; }

    [Column("FullName")]
    public string? FullName { get; set; }

    [Column("Email")]
    public string? Email { get; set; }

    [Column("Role")]
    public string? Role { get; set; }

    [Column("IsActive")]
    public bool IsActive { get; set; }

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; }
}