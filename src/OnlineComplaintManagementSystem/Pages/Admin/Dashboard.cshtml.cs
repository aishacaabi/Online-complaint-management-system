using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OnlineComplaintManagementSystem.Data;
using OnlineComplaintManagementSystem.Models;

namespace OnlineComplaintManagementSystem.Pages.Admin;

public class DashboardModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DashboardModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public int TotalComplaints { get; set; }
    public int NewThisWeek { get; set; }
    public int PendingCount { get; set; }
    public int InvestigatingCount { get; set; }
    public int ResolvedCount { get; set; }
    public int ClosedCount { get; set; }
    public int EscalatedCount { get; set; }
    public int TotalUsers { get; set; }
    public int TotalDepartments { get; set; }
    public double AverageResolutionDays { get; set; }
    public double AverageFeedbackRating { get; set; }
    public int FeedbackCount { get; set; }
    public DateTime WeekAgoDate { get; set; }

    public List<Complaint> RecentComplaints { get; set; } = new();

    public Dictionary<string, int> ByStatus { get; set; } = new();
    public Dictionary<string, int> ByCategory { get; set; } = new();
    public Dictionary<string, int> ByDepartment { get; set; } = new();
    public List<(string Month, int Count)> MonthlyTrend { get; set; } = new();

    public async Task OnGetAsync()
    {
        var complaints = await _context.Complaints
            .Include(c => c.Category)
            .Include(c => c.Department)
            .ToListAsync();

        TotalComplaints = complaints.Count;
        var weekAgo = DateTime.UtcNow.AddDays(-7);
        WeekAgoDate = weekAgo;
        NewThisWeek = complaints.Count(c => c.SubmittedDate >= weekAgo);
        PendingCount = complaints.Count(c => c.Status is ComplaintStatus.Submitted or ComplaintStatus.UnderReview or ComplaintStatus.Assigned or ComplaintStatus.PendingInformation);
        InvestigatingCount = complaints.Count(c => c.Status == ComplaintStatus.Investigating);
        // A resolved complaint that is later closed still counts as resolved.
        ResolvedCount = complaints.Count(c => c.ResolvedDate != null && c.Status is ComplaintStatus.Resolved or ComplaintStatus.Closed);
        ClosedCount = complaints.Count(c => c.Status == ComplaintStatus.Closed);
        EscalatedCount = complaints.Count(c => c.Status == ComplaintStatus.Escalated);

        TotalUsers = await _context.Users.CountAsync();
        TotalDepartments = await _context.Departments.CountAsync(d => d.IsActive);

        var resolved = complaints.Where(c => c.ResolvedDate.HasValue).ToList();
        AverageResolutionDays = resolved.Count > 0
            ? resolved.Average(c => (c.ResolvedDate!.Value - c.SubmittedDate).TotalDays)
            : 0;

        var feedbacks = await _context.Feedbacks.ToListAsync();
        FeedbackCount = feedbacks.Count;
        AverageFeedbackRating = feedbacks.Count > 0 ? feedbacks.Average(f => f.Rating) : 0;

        RecentComplaints = complaints.OrderByDescending(c => c.SubmittedDate).Take(8).ToList();

        ByStatus = complaints.GroupBy(c => c.Status.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        ByCategory = complaints.GroupBy(c => c.Category?.Name ?? "Uncategorized")
            .ToDictionary(g => g.Key, g => g.Count());

        ByDepartment = complaints.GroupBy(c => c.Department?.Name ?? "Unassigned")
            .ToDictionary(g => g.Key, g => g.Count());

        var trendStart = DateTime.UtcNow.AddMonths(-5);
        trendStart = new DateTime(trendStart.Year, trendStart.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        for (var i = 0; i < 6; i++)
        {
            var monthStart = trendStart.AddMonths(i);
            var monthEnd = monthStart.AddMonths(1);
            var count = complaints.Count(c => c.SubmittedDate >= monthStart && c.SubmittedDate < monthEnd);
            MonthlyTrend.Add((monthStart.ToString("MMM yyyy"), count));
        }
    }
}
