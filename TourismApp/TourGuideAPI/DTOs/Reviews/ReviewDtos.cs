namespace TourGuideAPI.DTOs.Reviews;

public record CreateReviewDto(
    int PlaceId,
    byte Rating,
    string? Comment,
    byte? TasteRating = null,
    byte? PriceRating = null,
    byte? SpaceRating = null
);

public record ReviewDto(
    int ReviewId,
    string UserName,
    string? UserAvatar,
    byte Rating,
    string? Comment,
    string? OwnerReply,
    DateTime CreatedAt,
    byte? TasteRating,
    byte? PriceRating,
    byte? SpaceRating
);