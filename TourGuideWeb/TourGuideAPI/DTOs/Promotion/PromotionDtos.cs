namespace TourGuideAPI.DTOs.Promotions;

public record CreatePromoDto(
    int PlaceId,
    string Title,
    string? Description,
    int? Discount,
    string? VoucherCode,
    DateTime StartDate,
    DateTime EndDate
);