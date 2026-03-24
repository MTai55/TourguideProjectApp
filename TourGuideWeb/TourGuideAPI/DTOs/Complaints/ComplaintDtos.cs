namespace TourGuideAPI.DTOs.Complaints;

public record CreateComplaintDto(
    int? PlaceId,
    int? ReviewId,
    string Type,
    string Title,
    string Content
);

public record ResolveComplaintDto(
    string Status,
    string Note
);