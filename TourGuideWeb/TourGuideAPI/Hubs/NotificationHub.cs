using Microsoft.AspNetCore.SignalR;

namespace TourGuideAPI.Hubs;

public class NotificationHub : Hub
{
    // Client join group theo OwnerId
    public async Task JoinOwnerGroup(string ownerId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, $"owner_{ownerId}");

    public async Task LeaveOwnerGroup(string ownerId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"owner_{ownerId}");
}

// Interface gửi notification từ service
public interface INotificationService
{
    Task SendNewCheckIn(int ownerId, string placeName, string userName);
    Task SendNewReview(int ownerId, string placeName, int rating);
}

public class NotificationService(IHubContext<NotificationHub> hub) : INotificationService
{
    public Task SendNewCheckIn(int ownerId, string placeName, string userName)
        => hub.Clients.Group($"owner_{ownerId}").SendAsync("NewCheckIn", new
        {
            message = $"{userName} vừa ghé {placeName}",
            placeName,
            userName,
            time = DateTime.UtcNow
        });

    public Task SendNewReview(int ownerId, string placeName, int rating)
        => hub.Clients.Group($"owner_{ownerId}").SendAsync("NewReview", new
        {
            message = $"Đánh giá mới {new string('★', rating)} cho {placeName}",
            placeName,
            rating,
            time = DateTime.UtcNow
        });
}