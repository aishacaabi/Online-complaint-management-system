using System.ComponentModel.DataAnnotations;

namespace OnlineComplaintManagementSystem.Models;

public class ComplaintResponse
{
    public int Id { get; set; }

    public int ComplaintId { get; set; }
    public Complaint? Complaint { get; set; }

    [Required]
    public string AuthorId { get; set; } = string.Empty;
    public ApplicationUser? Author { get; set; }

    public ResponseAuthorType AuthorType { get; set; }

    [Required, StringLength(4000)]
    public string Message { get; set; } = string.Empty;

    /// <summary>True when this message is a formal request for additional information from the complainant.</summary>
    public bool IsInformationRequest { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public ICollection<ComplaintAttachment> Attachments { get; set; } = new List<ComplaintAttachment>();
}
