namespace TourGuideAPI.Models;

public class Message
{
    public int MessageId { get; set; }
    public int PlaceId { get; set; }
    public int UserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsFromOwner { get; set; }
    public int? ParentId { get; set; }
    public bool IsPublic { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Place? Place { get; set; }
    public User? User { get; set; }
    public Message? Parent { get; set; }
}