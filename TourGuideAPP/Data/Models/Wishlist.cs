using Postgrest.Attributes;
using Postgrest.Models;

namespace TourGuideAPP.Data.Models;

[Table("Wishlist")]
public class WishlistItem : BaseModel
{
    [PrimaryKey("WishlistId")]
    public int WishlistId { get; set; }

    [Column("UserId")]
    public int UserId { get; set; }

    [Column("PlaceId")]
    public int PlaceId { get; set; }

    [Column("Note")]
    public string? Note { get; set; }

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; }
}