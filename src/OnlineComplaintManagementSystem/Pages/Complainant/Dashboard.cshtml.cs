using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OnlineComplaintManagementSystem.Data;
using OnlineComplaintManagementSystem.Models;

namespace OnlineComplaintManagementSystem.Pages.Complainant;

public class DashboardModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public int TotalComplaints { get; set; }
    public int SubmittedCount { get; set; }
    public int PendingCount { get; set; }
    public int ResolvedCount { get; set; }
    public int ClosedCount { get; set; }
    public int RejectedCount { get; set; }
    public List<Complaint> RecentComplaints { get; set; } = new();

    public async Task OnGetAsync()
    {
        var userId = _userManager.GetUserId(User)!;
        var complaints = await _context.Complaints
            .Include(c => c.Category)
            .Include(c => c.Department)
            .Where(c => c.ComplainantId == userId)
            .OrderByDescending(c => c.SubmittedDate)
            .ToListAsync();

        TotalComplaints = complaints.Count;
        SubmittedCount = complaints.Count(c => c.Status == ComplaintStatus.Submitted);
        PendingCount = complaints.Count(c => c.Status is ComplaintStatus.UnderReview or ComplaintStatus.Assigned or ComplaintStatus.Investigating or ComplaintStatus.PendingInformation or ComplaintStatus.Escalated);
        // A resolved complaint that is later closed still counts as resolved.
        ResolvedCount = complaints.Count(c => c.ResolvedDate != null && c.Status is ComplaintStatus.Resolved or ComplaintStatus.Closed);
        ClosedCount = complaints.Count(c => c.Status == ComplaintStatus.Closed);
        RejectedCount = complaints.Count(c => c.Status == ComplaintStatus.Rejected);

        RecentComplaints = complaints.Take(5).ToList();
    }
}
