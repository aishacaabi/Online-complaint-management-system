using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OnlineComplaintManagementSystem.Data;
using OnlineComplaintManagementSystem.Models;

namespace OnlineComplaintManagementSystem.Pages.Officer.Complaints;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private const int PageSize = 10;

    public IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public List<Complaint> Complaints { get; set; } = new();

    public string? SearchTerm { get; set; }
    public ComplaintStatus? StatusFilter { get; set; }
    public ComplaintPriority? PriorityFilter { get; set; }
    public string? StatusGroup { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }

    private static readonly ComplaintStatus[] PendingStatuses =
    {
        ComplaintStatus.Assigned, ComplaintStatus.UnderReview, ComplaintStatus.PendingInformation
    };

    public async Task OnGetAsync(string? search, ComplaintStatus? status, ComplaintPriority? priority, string? statusGroup, int page = 1)
    {
        SearchTerm = search;
        StatusFilter = status;
        PriorityFilter = priority;
        StatusGroup = statusGroup;
        CurrentPage = page < 1 ? 1 : page;

        var userId = _userManager.GetUserId(User)!;
        var query = _context.Complaints
            .Include(c => c.Category)
            .Include(c => c.Department)
            .Include(c => c.Complainant)
            .Where(c => c.Assignments.Any(a => a.OfficerId == userId && a.UnassignedDate == null));

        if (!string.IsNullOrWhiteSpace(search))
        {
            // Lower-cased on both sides so the search ignores case on every database provider.
            var term = search.Trim().ToLower();
            query = query.Where(c => c.Title.ToLower().Contains(term) || c.ReferenceNumber.ToLower().Contains(term));
        }
        if (statusGroup == "pending")
        {
            query = query.Where(c => PendingStatuses.Contains(c.Status));
        }
        else if (statusGroup == "overdue")
        {
            var now = DateTime.UtcNow;
            query = query.Where(c => c.ExpectedResolutionDate.HasValue && c.ExpectedResolutionDate < now
                && c.Status != ComplaintStatus.Resolved && c.Status != ComplaintStatus.Closed && c.Status != ComplaintStatus.Rejected);
        }
        else if (statusGroup == "resolved")
        {
            query = query.Where(c => c.ResolvedDate != null && (c.Status == ComplaintStatus.Resolved || c.Status == ComplaintStatus.Closed));
        }
        else if (status.HasValue)
        {
            query = query.Where(c => c.Status == status);
        }
        if (priority.HasValue)
        {
            query = query.Where(c => c.Priority == priority);
        }

        var totalCount = await query.CountAsync();
        TotalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)PageSize));
        CurrentPage = Math.Min(CurrentPage, TotalPages);

        Complaints = await query
            .OrderByDescending(c => c.SubmittedDate)
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
    }
}
