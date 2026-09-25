using System.ComponentModel.DataAnnotations;

namespace OnlineComplaintManagementSystem.Models;

public class Department
{
    public int Id { get; set; }

    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(100)]
    public string? ContactEmail { get; set; }

    [StringLength(30)]
    public string? ContactPhone { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; }

    public ICollection<ApplicationUser> Officers { get; set; } = new List<ApplicationUser>();
    public ICollection<Complaint> Complaints { get; set; } = new List<Complaint>();
}
