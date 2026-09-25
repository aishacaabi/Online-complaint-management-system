namespace OnlineComplaintManagementSystem.Services;

public class ComplaintReportRow
{
    public string ReferenceNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string AssignedOfficer { get; set; } = "Unassigned";
    public DateTime SubmittedDate { get; set; }
    public DateTime? ResolvedDate { get; set; }
}
