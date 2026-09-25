using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OnlineComplaintManagementSystem.Data;
using OnlineComplaintManagementSystem.Models;

namespace OnlineComplaintManagementSystem.Pages.Officer;

public class DashboardModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public DashboardModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public int AssignedCount { get; set; }
    public int PendingCount { get; set; }
    public int InvestigatingCount { get; set; }
    public int ResolvedCount { get; set; }
    public int OverdueCount { get; set; }
    public List<Complaint> RecentAssigned { get; set; } = new();

    public async Task OnGetAsync()
    {
        var userId = _userManager.GetUserId(User)!;

        var complaints = await _context.Complaints
            .Include(c => c.Category)
            .Include(c => c.Department)
            .Include(c => c.Assignments)
            .Where(c => c.Assignments.Any(a => a.OfficerId == userId && a.UnassignedDate == null))
            .OrderByDescending(c => c.SubmittedDate)
            .ToListAsync();

        AssignedCount = complaints.Count;
        PendingCount = complaints.Count(c => c.Status is ComplaintStatus.Assigned or ComplaintStatus.UnderReview or ComplaintStatus.PendingInformation);
        InvestigatingCount = complaints.Count(c => c.Status == ComplaintStatus.Investigating);
        // A resolved complaint that is later closed still counts as resolved.
        ResolvedCount = complaints.Count(c => c.ResolvedDate != null && c.Status is ComplaintStatus.Resolved or ComplaintStatus.Closed);
        OverdueCount = complaints.Count(c => c.ExpectedResolutionDate.HasValue
            && c.ExpectedResolutionDate < DateTime.UtcNow
            && c.Status != ComplaintStatus.Resolved
            && c.Status != ComplaintStatus.Closed
            && c.Status != ComplaintStatus.Rejected);

        RecentAssigned = complaints.Take(8).ToList();
    }
}
