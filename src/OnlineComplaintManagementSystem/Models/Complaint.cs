using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineComplaintManagementSystem.Models;

public class Complaint
{
    public int Id { get; set; }

    [Required, StringLength(30)]
    public string ReferenceNumber { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(4000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public int CategoryId { get; set; }
    public ComplaintCategory? Category { get; set; }

    [Required]
    public int DepartmentId { get; set; }
    public Department? Department { get; set; }

    public ComplaintPriority Priority { get; set; } = ComplaintPriority.Medium;
    public ComplaintStatus Status { get; set; } = ComplaintStatus.Submitted;

    [StringLength(250)]
    public string? Location { get; set; }

    [Required]
    public string ComplainantId { get; set; } = string.Empty;
    public ApplicationUser? Complainant { get; set; }

    [StringLength(150)]
    public string? ContactName { get; set; }
    [StringLength(30)]
    public string? ContactPhone { get; set; }
    [StringLength(150)]
    public string? ContactEmail { get; set; }

    public DateTime SubmittedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; }
    public DateTime? ExpectedResolutionDate { get; set; }
    public DateTime? ResolvedDate { get; set; }
    public DateTime? ClosedDate { get; set; }

    [StringLength(2000)]
    public string? ResolutionSummary { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public ICollection<ComplaintAttachment> Attachments { get; set; } = new List<ComplaintAttachment>();
    public ICollection<ComplaintAssignment> Assignments { get; set; } = new List<ComplaintAssignment>();
    public ICollection<ComplaintStatusHistory> StatusHistory { get; set; } = new List<ComplaintStatusHistory>();
    public ICollection<ComplaintResponse> Responses { get; set; } = new List<ComplaintResponse>();
    public Feedback? Feedback { get; set; }

    [NotMapped]
    public ApplicationUser? CurrentOfficer => Assignments
        .Where(a => a.UnassignedDate == null)
        .OrderByDescending(a => a.AssignedDate)
        .Select(a => a.Officer)
        .FirstOrDefault();
}
