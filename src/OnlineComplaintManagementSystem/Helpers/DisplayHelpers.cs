using OnlineComplaintManagementSystem.Models;

namespace OnlineComplaintManagementSystem.Helpers;

public static class DisplayHelpers
{
    public static string BadgeClass(this ComplaintStatus status) => status switch
    {
        ComplaintStatus.Submitted => "bg-secondary",
        ComplaintStatus.UnderReview => "bg-info text-dark",
        ComplaintStatus.Assigned => "bg-primary",
        ComplaintStatus.Investigating => "bg-primary",
        ComplaintStatus.PendingInformation => "bg-warning text-dark",
        ComplaintStatus.Escalated => "bg-danger",
        ComplaintStatus.Resolved => "bg-success",
        ComplaintStatus.Closed => "bg-dark",
        ComplaintStatus.Rejected => "bg-danger",
        _ => "bg-secondary"
    };

    public static string DisplayName(this ComplaintStatus status) => status switch
    {
        ComplaintStatus.PendingInformation => "Pending Information",
        ComplaintStatus.UnderReview => "Under Review",
        _ => status.ToString()
    };

    public static string BadgeClass(this ComplaintPriority priority) => priority switch
    {
        ComplaintPriority.Low => "bg-success",
        ComplaintPriority.Medium => "bg-info text-dark",
        ComplaintPriority.High => "bg-warning text-dark",
        ComplaintPriority.Critical => "bg-danger",
        _ => "bg-secondary"
    };

    public static string IconClass(this NotificationType type) => type switch
    {
        NotificationType.ComplaintSubmitted => "bi-file-earmark-plus",
        NotificationType.ComplaintAssigned => "bi-person-check",
        NotificationType.StatusChanged => "bi-arrow-repeat",
        NotificationType.InformationRequested => "bi-question-circle",
        NotificationType.ComplaintEscalated => "bi-exclamation-triangle",
        NotificationType.ComplaintResolved => "bi-check-circle",
        NotificationType.ComplaintClosed => "bi-lock",
        NotificationType.NewResponse => "bi-chat-dots",
        _ => "bi-bell"
    };
}
