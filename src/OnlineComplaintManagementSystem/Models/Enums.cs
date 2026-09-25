namespace OnlineComplaintManagementSystem.Models;

public enum ComplaintStatus
{
    Submitted = 0,
    UnderReview = 1,
    Assigned = 2,
    Investigating = 3,
    PendingInformation = 4,
    Escalated = 5,
    Resolved = 6,
    Closed = 7,
    Rejected = 8
}

public enum ComplaintPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

public enum ResponseAuthorType
{
    ComplaintOfficer = 0,
    Complainant = 1,
    System = 2
}

public enum NotificationType
{
    ComplaintSubmitted = 0,
    ComplaintAssigned = 1,
    StatusChanged = 2,
    InformationRequested = 3,
    ComplaintEscalated = 4,
    ComplaintResolved = 5,
    ComplaintClosed = 6,
    NewResponse = 7
}

public static class RoleNames
{
    public const string SuperAdministrator = "SuperAdministrator";
    public const string ComplaintOfficer = "ComplaintOfficer";
    public const string Complainant = "Complainant";
}
