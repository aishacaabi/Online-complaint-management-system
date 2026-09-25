using System.ComponentModel.DataAnnotations;

namespace OnlineComplaintManagementSystem.Models;

public class Notification
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public NotificationType Type { get; set; }

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string Message { get; set; } = string.Empty;

    public int? ComplaintId { get; set; }
    public Complaint? Complaint { get; set; }

    public bool IsRead { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ReadDate { get; set; }
}
