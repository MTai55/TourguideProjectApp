namespace TourGuideAPI.DTOs.Reviews;

public record CreateReviewDto(
    int PlaceId,
    byte Rating,
    string? Comment
);

public record ReviewDto(
    int ReviewId,
    string UserName,
    string? UserAvatar,
    byte Rating,
    string? Comment,
    string? OwnerReply,
    DateTime CreatedAt
);