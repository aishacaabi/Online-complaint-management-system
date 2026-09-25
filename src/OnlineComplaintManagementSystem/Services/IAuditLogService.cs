namespace OnlineComplaintManagementSystem.Services;

public interface IAuditLogService
{
    Task LogAsync(string? userId, string userName, string action, string? description = null, string? ipAddress = null);
}
