using System.ComponentModel.DataAnnotations;

namespace OnlineComplaintManagementSystem.Models;

public class AuditLog
{
    public int Id { get; set; }

    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    [Required, StringLength(100)]
    public string UserName { get; set; } = "System";

    [Required, StringLength(100)]
    public string Action { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [StringLength(50)]
    public string? IpAddress { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
