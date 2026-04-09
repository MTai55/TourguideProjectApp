using Postgrest.Attributes;
using Postgrest.Models;

namespace TourGuideAPP.Data.Models;

[Table("PlaceTtsContents")]
public class PlaceTtsContent : BaseModel
{
    [PrimaryKey("Id", false)]
    public int Id { get; set; }

    [Column("PlaceId")]
    public int PlaceId { get; set; }

    [Column("Locale")]
    public string Locale { get; set; } = string.Empty;

    [Column("Script")]
    public string Script { get; set; } = string.Empty;
}
