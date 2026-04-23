namespace TourGuideAPI.DTOs.Places;

public class PlaceTtsContentDto
{
    public int Id { get; set; }
    public int PlaceId { get; set; }
    public string Locale { get; set; } = string.Empty;
    public string Script { get; set; } = string.Empty;
}

public class CreatePlaceTtsContentDto
{
    public string Locale { get; set; } = string.Empty;
    public string Script { get; set; } = string.Empty;
}

public class UpdatePlaceTtsContentDto
{
    public string Script { get; set; } = string.Empty;
}

public class PlaceTtsContentListDto
{
    public int PlaceId { get; set; }
    public List<PlaceTtsContentDto> Contents { get; set; } = [];
}
