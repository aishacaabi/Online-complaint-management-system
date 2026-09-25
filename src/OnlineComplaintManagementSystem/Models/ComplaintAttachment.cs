using System.ComponentModel.DataAnnotations;

namespace OnlineComplaintManagementSystem.Models;

public class ComplaintAttachment
{
    public int Id { get; set; }

    [Required]
    public int ComplaintId { get; set; }
    public Complaint? Complaint { get; set; }

    [Required, StringLength(260)]
    public string FileName { get; set; } = string.Empty;

    [Required, StringLength(400)]
    public string StoredPath { get; set; } = string.Empty;

    [StringLength(100)]
    public string ContentType { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    [Required]
    public string UploadedById { get; set; } = string.Empty;
    public ApplicationUser? UploadedBy { get; set; }

    public DateTime UploadedDate { get; set; } = DateTime.UtcNow;

    /// <summary>Optional link to a specific response/communication entry.</summary>
    public int? ComplaintResponseId { get; set; }
    public ComplaintResponse? ComplaintResponse { get; set; }
}
