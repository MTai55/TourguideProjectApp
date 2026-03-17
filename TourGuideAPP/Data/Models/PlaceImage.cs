using Postgrest.Attributes;
using Postgrest.Models;

namespace TourGuideAPP.Data.Models;

[Table("PlaceImages")]
public class PlaceImage : BaseModel
{
    [PrimaryKey("ImageId")]
    public int ImageId { get; set; }

    [Column("PlaceId")]
    public int PlaceId { get; set; }

    [Column("ImageUrl")]
    public string ImageUrl { get; set; } = string.Empty;

    [Column("IsMain")]
    public bool IsMain { get; set; }

    [Column("SortOrder")]
    public int SortOrder { get; set; }
}