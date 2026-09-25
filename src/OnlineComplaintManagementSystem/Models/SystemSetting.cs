using System.ComponentModel.DataAnnotations;

namespace OnlineComplaintManagementSystem.Models;

/// <summary>Singleton row (Id = 1) holding editable, application-wide configuration.</summary>
public class SystemSetting
{
    public int Id { get; set; }

    [Required, StringLength(150)]
    public string OrganizationName { get; set; } = "Online Complaint Management System";

    [Range(1, 90)]
    public int DefaultResolutionDays { get; set; } = 14;

    [StringLength(100)]
    public string? SupportEmail { get; set; }

    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
}
