namespace TourGuideAPI.DTOs.Places;

public record PlaceDto(
    int PlaceId,
    string Name,
    string? Description,
    string Address,
    double Latitude,
    double Longitude,
    string? Phone,
    string? OpenTime,
    string? CloseTime,
    double AverageRating,
    int TotalReviews,
    int TotalVisits,
    string? CategoryName,
    string? MainImageUrl,
    double? DistanceKm,
    string? Specialty,
    int? PricePerPerson,
    decimal? PriceMin,
    decimal? PriceMax,
    string? District,
    bool HasParking,
    bool HasAircon
);

public record CreatePlaceDto(
    string Name,
    string? Description,
    string Address,
    double Latitude,
    double Longitude,
    string? Phone,
    int?     CategoryId,
    decimal? PriceMin,
    decimal? PriceMax,
    string? Specialty,
    int? PricePerPerson,
    string? District,
    string? OpenTime,
    string? CloseTime,
    bool HasParking = false,
    bool HasAircon = false
);

public record NearbyQueryDto(
    double Lat,
    double Lng,
    double RadiusKm = 5.0,
    int? CategoryId = null,
    int Page = 1,
    int PageSize = 20
);