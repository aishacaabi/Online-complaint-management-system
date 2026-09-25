using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OnlineComplaintManagementSystem.Data;
using OnlineComplaintManagementSystem.Models;
using OnlineComplaintManagementSystem.Services;

namespace OnlineComplaintManagementSystem.Pages.Notifications;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly INotificationService _notificationService;

    public IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, INotificationService notificationService)
    {
        _context = context;
        _userManager = userManager;
        _notificationService = notificationService;
    }

    public List<Notification> Notifications { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? openId)
    {
        var userId = _userManager.GetUserId(User);

        if (openId.HasValue)
        {
            var target = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == openId.Value && n.UserId == userId);
            if (target is not null)
            {
                await _notificationService.MarkAsReadAsync(openId.Value, userId!);
                if (target.ComplaintId is not null)
                {
                    return Redirect(ResolveComplaintUrl(target.ComplaintId.Value));
                }
            }
        }

        Notifications = await _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedDate)
            .Take(100)
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostMarkAllReadAsync()
    {
        var userId = _userManager.GetUserId(User);
        await _notificationService.MarkAllAsReadAsync(userId!);
        return RedirectToPage();
    }

    private string ResolveComplaintUrl(int complaintId)
    {
        if (User.IsInRole(RoleNames.SuperAdministrator))
        {
            return Url.Page("/Admin/Complaints/Details", new { id = complaintId }) ?? "/Notifications";
        }
        if (User.IsInRole(RoleNames.ComplaintOfficer))
        {
            return Url.Page("/Officer/Complaints/Details", new { id = complaintId }) ?? "/Notifications";
        }
        return Url.Page("/Complainant/Complaints/Details", new { id = complaintId }) ?? "/Notifications";
    }
}
