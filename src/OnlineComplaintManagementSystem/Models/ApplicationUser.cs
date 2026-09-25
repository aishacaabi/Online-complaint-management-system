using Microsoft.AspNetCore.Identity;

namespace OnlineComplaintManagementSystem.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>Web-relative path (e.g. "/uploads/avatars/xxxx.jpg") to the user's profile photo, or null if none set.</summary>
    public string? ProfilePhotoPath { get; set; }

    /// <summary>Only meaningful for ComplaintOfficer users.</summary>
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public ICollection<Complaint> SubmittedComplaints { get; set; } = new List<Complaint>();
    public ICollection<ComplaintAssignment> Assignments { get; set; } = new List<ComplaintAssignment>();
}
