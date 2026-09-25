namespace OnlineComplaintManagementSystem.Models;

public class ComplaintStatusHistory
{
    public int Id { get; set; }

    public int ComplaintId { get; set; }
    public Complaint? Complaint { get; set; }

    public ComplaintStatus? PreviousStatus { get; set; }
    public ComplaintStatus NewStatus { get; set; }

    public string ChangedById { get; set; } = string.Empty;
    public ApplicationUser? ChangedBy { get; set; }

    public string? Notes { get; set; }

    public DateTime ChangedDate { get; set; } = DateTime.UtcNow;
}
