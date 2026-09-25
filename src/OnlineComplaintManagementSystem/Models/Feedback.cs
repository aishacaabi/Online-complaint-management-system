using System.ComponentModel.DataAnnotations;

namespace OnlineComplaintManagementSystem.Models;

public class Feedback
{
    public int Id { get; set; }

    [Required]
    public int ComplaintId { get; set; }
    public Complaint? Complaint { get; set; }

    [Required]
    public string SubmittedById { get; set; } = string.Empty;
    public ApplicationUser? SubmittedBy { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; }

    [StringLength(1000)]
    public string? Comments { get; set; }

    public DateTime SubmittedDate { get; set; } = DateTime.UtcNow;
}
