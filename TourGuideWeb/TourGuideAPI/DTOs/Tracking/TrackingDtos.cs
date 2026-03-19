namespace TourGuideAPI.DTOs.Tracking;

public record LocationDto(
    double Latitude,
    double Longitude,
    double? Accuracy = null
);

public record CheckInDto(
    int PlaceId,
    double Latitude,
    double Longitude,
    bool AutoDetected = false,
    string? Notes = null
);

public record TripStatsDto(
    int TotalVisits,
    int UniquePlaces,
    double TotalDistanceKm,
    int TotalMinutesSpent,
    List<VisitSummaryDto> RecentVisits
);

public record VisitSummaryDto(
    int VisitId,
    int PlaceId,
    string PlaceName,
    string? PlaceImage,
    DateTime CheckInTime,
    int? DurationMins
);