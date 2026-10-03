using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OnlineComplaintManagementSystem.Data;
using OnlineComplaintManagementSystem.Models;

namespace OnlineComplaintManagementSystem.Pages.Admin.Complaints;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private const int PageSize = 15;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<Complaint> Complaints { get; set; } = new();
    public List<ComplaintCategory> Categories { get; set; } = new();
    public List<Department> Departments { get; set; } = new();
    public List<ApplicationUser> Officers { get; set; } = new();

    public string? SearchTerm { get; set; }
    public ComplaintStatus? StatusFilter { get; set; }
    public ComplaintPriority? PriorityFilter { get; set; }
    public int? CategoryFilter { get; set; }
    public int? DepartmentFilter { get; set; }
    public string? OfficerFilter { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? StatusGroup { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }

    private static readonly ComplaintStatus[] PendingStatuses =
    {
        ComplaintStatus.Submitted, ComplaintStatus.UnderReview, ComplaintStatus.Assigned, ComplaintStatus.PendingInformation
    };

    public async Task OnGetAsync(string? search, ComplaintStatus? status, ComplaintPriority? priority,
        int? categoryId, int? departmentId, string? officerId, DateTime? fromDate, DateTime? toDate, string? statusGroup, int page = 1)
    {
        SearchTerm = search;
        StatusFilter = status;
        PriorityFilter = priority;
        CategoryFilter = categoryId;
        DepartmentFilter = departmentId;
        OfficerFilter = officerId;
        FromDate = fromDate;
        ToDate = toDate;
        StatusGroup = statusGroup;
        CurrentPage = page < 1 ? 1 : page;

        Categories = await _context.ComplaintCategories.OrderBy(c => c.Name).ToListAsync();
        Departments = await _context.Departments.OrderBy(d => d.Name).ToListAsync();
        Officers = await _context.Users
            .Where(u => _context.UserRoles.Any(ur => ur.UserId == u.Id &&
                _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == RoleNames.ComplaintOfficer)))
            .OrderBy(u => u.FullName)
            .ToListAsync();

        var query = _context.Complaints
            .Include(c => c.Category)
            .Include(c => c.Department)
            .Include(c => c.Complainant)
            .Include(c => c.Assignments).ThenInclude(a => a.Officer)
            .AsQueryable();

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
        else if (statusGroup == "resolved")
        {
            query = query.Where(c => c.ResolvedDate != null && (c.Status == ComplaintStatus.Resolved || c.Status == ComplaintStatus.Closed));
        }
        else if (status.HasValue)
        {
            query = query.Where(c => c.Status == status);
        }
        if (priority.HasValue) query = query.Where(c => c.Priority == priority);
        if (categoryId.HasValue) query = query.Where(c => c.CategoryId == categoryId);
        if (departmentId.HasValue) query = query.Where(c => c.DepartmentId == departmentId);
        if (!string.IsNullOrEmpty(officerId))
        {
            query = query.Where(c => c.Assignments.Any(a => a.OfficerId == officerId && a.UnassignedDate == null));
        }
        if (fromDate.HasValue) query = query.Where(c => c.SubmittedDate >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(c => c.SubmittedDate < toDate.Value.AddDays(1));

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
