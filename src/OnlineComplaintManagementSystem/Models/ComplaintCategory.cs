using System.ComponentModel.DataAnnotations;

namespace OnlineComplaintManagementSystem.Models;

public class ComplaintCategory
{
    public int Id { get; set; }

    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; }

    public ICollection<Complaint> Complaints { get; set; } = new List<Complaint>();
}
