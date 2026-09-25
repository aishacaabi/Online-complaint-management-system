using OnlineComplaintManagementSystem.Models;

namespace OnlineComplaintManagementSystem.ViewModels;

public class ComplaintDetailsViewModel
{
    public Complaint Complaint { get; set; } = null!;

    // Permissions - control which action panels render for the current role.
    public bool CanAssign { get; set; }
    public bool CanChangeStatus { get; set; }
    public bool CanChangePriority { get; set; }
    public bool CanEscalate { get; set; }
    public bool CanResolve { get; set; }
    public bool CanClose { get; set; }
    public bool CanReject { get; set; }
    public bool CanReply { get; set; }
    public bool CanRequestInfo { get; set; }
    public bool CanUploadAttachment { get; set; }
    public bool CanGiveFeedback { get; set; }
    public bool ShowContactInfo { get; set; }

    public List<(string Id, string Name)> AvailableOfficers { get; set; } = new();
}
