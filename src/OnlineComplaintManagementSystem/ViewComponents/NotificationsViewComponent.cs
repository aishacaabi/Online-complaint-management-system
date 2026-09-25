using Microsoft.AspNetCore.Mvc;
using OnlineComplaintManagementSystem.Models;
using OnlineComplaintManagementSystem.Services;

namespace OnlineComplaintManagementSystem.ViewComponents;

public class NotificationsPanelModel
{
    public int UnreadCount { get; set; }
    public List<Notification> Recent { get; set; } = new();
}

public class NotificationsViewComponent : ViewComponent
{
    private readonly INotificationService _notificationService;

    public NotificationsViewComponent(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userId = HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return View("Default", new NotificationsPanelModel());
        }

        var model = new NotificationsPanelModel
        {
            UnreadCount = await _notificationService.GetUnreadCountAsync(userId),
            Recent = await _notificationService.GetRecentAsync(userId, 8)
        };
        return View("Default", model);
    }
}
