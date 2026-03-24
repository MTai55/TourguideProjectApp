namespace TourGuideAPI.DTOs.Messages;

public record CreateMessageDto(
    int PlaceId,
    string Content
);