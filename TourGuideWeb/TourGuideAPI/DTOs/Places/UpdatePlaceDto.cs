namespace TourGuideAPI.DTOs.Places;

public record UpdatePlaceDto(
    string Name,
    string? Description,
    string Address,
    string? Phone,
    string? OpenTime,
    string? CloseTime,
    string? Specialty,
    int? PricePerPerson,
    decimal? PriceMin,
    decimal? PriceMax
);

public record AddImageDto(
    string ImageUrl,
    bool IsMain = false
);