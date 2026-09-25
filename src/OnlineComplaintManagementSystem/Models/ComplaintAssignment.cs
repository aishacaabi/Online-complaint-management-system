namespace OnlineComplaintManagementSystem.Models;

public class ComplaintAssignment
{
    public int Id { get; set; }

    public int ComplaintId { get; set; }
    public Complaint? Complaint { get; set; }

    public string OfficerId { get; set; } = string.Empty;
    public ApplicationUser? Officer { get; set; }

    public string AssignedById { get; set; } = string.Empty;
    public ApplicationUser? AssignedBy { get; set; }

    public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UnassignedDate { get; set; }

    public string? Notes { get; set; }
}
