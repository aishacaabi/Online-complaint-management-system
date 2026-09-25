using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OnlineComplaintManagementSystem.Data;
using OnlineComplaintManagementSystem.Models;

namespace OnlineComplaintManagementSystem.Pages.Admin.AuditLogs;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private const int PageSize = 25;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<AuditLog> Logs { get; set; } = new();
    public List<string> Actions { get; set; } = new();

    public string? SearchTerm { get; set; }
    public string? ActionFilter { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }

    public async Task OnGetAsync(string? search, string? action, DateTime? fromDate, DateTime? toDate, int page = 1)
    {
        SearchTerm = search;
        ActionFilter = action;
        FromDate = fromDate;
        ToDate = toDate;
        CurrentPage = page < 1 ? 1 : page;

        Actions = await _context.AuditLogs.Select(a => a.Action).Distinct().OrderBy(a => a).ToListAsync();

        var query = _context.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(a => a.UserName.Contains(search) || (a.Description != null && a.Description.Contains(search)));
        }
        if (!string.IsNullOrEmpty(action))
        {
            query = query.Where(a => a.Action == action);
        }
        if (fromDate.HasValue) query = query.Where(a => a.Timestamp >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(a => a.Timestamp < toDate.Value.AddDays(1));

        var totalCount = await query.CountAsync();
        TotalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)PageSize));
        CurrentPage = Math.Min(CurrentPage, TotalPages);

        Logs = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
    }
}
