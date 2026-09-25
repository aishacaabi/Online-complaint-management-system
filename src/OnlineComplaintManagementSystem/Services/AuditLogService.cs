using OnlineComplaintManagementSystem.Data;
using OnlineComplaintManagementSystem.Models;

namespace OnlineComplaintManagementSystem.Services;

public class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _context;

    public AuditLogService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(string? userId, string userName, string action, string? description = null, string? ipAddress = null)
    {
        _context.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            UserName = string.IsNullOrWhiteSpace(userName) ? "System" : userName,
            Action = action,
            Description = description,
            IpAddress = ipAddress,
            Timestamp = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
    }
}
