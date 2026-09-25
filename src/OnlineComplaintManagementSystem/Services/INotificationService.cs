using OnlineComplaintManagementSystem.Models;

namespace OnlineComplaintManagementSystem.Services;

public interface INotificationService
{
    Task CreateAsync(string userId, NotificationType type, string title, string message, int? complaintId = null);
    Task<List<Notification>> GetRecentAsync(string userId, int count = 8);
    Task<int> GetUnreadCountAsync(string userId);
    Task MarkAsReadAsync(int notificationId, string userId);
    Task MarkAllAsReadAsync(string userId);
}
