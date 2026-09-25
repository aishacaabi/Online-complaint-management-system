using Microsoft.EntityFrameworkCore;
using OnlineComplaintManagementSystem.Data;
using OnlineComplaintManagementSystem.Models;

namespace OnlineComplaintManagementSystem.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;

    public NotificationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task CreateAsync(string userId, NotificationType type, string title, string message, int? complaintId = null)
    {
        _context.Notifications.Add(new Notification
        {
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            ComplaintId = complaintId,
            CreatedDate = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
    }

    public async Task<List<Notification>> GetRecentAsync(string userId, int count = 8)
    {
        return await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedDate)
            .Take(count)
            .ToListAsync();
    }

    public async Task<int> GetUnreadCountAsync(string userId)
    {
        return await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task MarkAsReadAsync(int notificationId, string userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
        if (notification is not null && !notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task MarkAllAsReadAsync(string userId)
    {
        var unread = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();
        foreach (var n in unread)
        {
            n.IsRead = true;
            n.ReadDate = DateTime.UtcNow;
        }
        await _context.SaveChangesAsync();
    }
}
