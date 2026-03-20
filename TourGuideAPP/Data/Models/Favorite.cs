using Postgrest.Attributes;
using Postgrest.Models;

namespace TourGuideAPP.Data.Models;

[Table("Favorites")]
public class Favorite : BaseModel
{
    [PrimaryKey("FavoriteId")]
    public int FavoriteId { get; set; }

    [Column("UserId")]
    public int UserId { get; set; }

    [Column("PlaceId")]
    public int PlaceId { get; set; }

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; }
}