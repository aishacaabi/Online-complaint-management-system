using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OnlineComplaintManagementSystem.Data;
using OnlineComplaintManagementSystem.Models;
using OnlineComplaintManagementSystem.Services;

namespace OnlineComplaintManagementSystem.Pages.Admin.Reports;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IReportExportService _reportExportService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditLogService _auditLogService;

    public IndexModel(ApplicationDbContext context, IReportExportService reportExportService, UserManager<ApplicationUser> userManager, IAuditLogService auditLogService)
    {
        _context = context;
        _reportExportService = reportExportService;
        _userManager = userManager;
        _auditLogService = auditLogService;
    }

    public List<Complaint> Results { get; set; } = new();
    public List<ComplaintCategory> Categories { get; set; } = new();
    public List<Department> Departments { get; set; } = new();
    public List<ApplicationUser> Officers { get; set; } = new();

    public string? ReportTitle { get; set; } = "Complaint Summary Report";
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? DepartmentFilter { get; set; }
    public int? CategoryFilter { get; set; }
    public ComplaintStatus? StatusFilter { get; set; }
    public ComplaintPriority? PriorityFilter { get; set; }
    public string? OfficerFilter { get; set; }

    public double AverageResolutionDays { get; set; }

    public async Task OnGetAsync(string? reportTitle, DateTime? fromDate, DateTime? toDate, int? departmentId,
        int? categoryId, ComplaintStatus? status, ComplaintPriority? priority, string? officerId)
    {
        ReportTitle = string.IsNullOrWhiteSpace(reportTitle) ? "Complaint Summary Report" : reportTitle;
        FromDate = fromDate;
        ToDate = toDate;
        DepartmentFilter = departmentId;
        CategoryFilter = categoryId;
        StatusFilter = status;
        PriorityFilter = priority;
        OfficerFilter = officerId;

        await LoadDropdownsAsync();
        Results = await BuildQuery().OrderByDescending(c => c.SubmittedDate).ToListAsync();

        var resolved = Results.Where(c => c.ResolvedDate.HasValue).ToList();
        AverageResolutionDays = resolved.Count > 0
            ? resolved.Average(c => (c.ResolvedDate!.Value - c.SubmittedDate).TotalDays)
            : 0;
    }

    public async Task<IActionResult> OnGetExportExcelAsync(string? reportTitle, DateTime? fromDate, DateTime? toDate,
        int? departmentId, int? categoryId, ComplaintStatus? status, ComplaintPriority? priority, string? officerId)
    {
        ReportTitle = string.IsNullOrWhiteSpace(reportTitle) ? "Complaint Summary Report" : reportTitle;
        FromDate = fromDate; ToDate = toDate; DepartmentFilter = departmentId; CategoryFilter = categoryId;
        StatusFilter = status; PriorityFilter = priority; OfficerFilter = officerId;

        var rows = await BuildQuery().OrderByDescending(c => c.SubmittedDate).ToListAsync();
        var reportRows = rows.Select(ToReportRow).ToList();
        var bytes = _reportExportService.ExportToExcel(ReportTitle, reportRows);

        await LogExportAsync("Excel");
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{SafeFileName(ReportTitle)}.xlsx");
    }

    public async Task<IActionResult> OnGetExportPdfAsync(string? reportTitle, DateTime? fromDate, DateTime? toDate,
        int? departmentId, int? categoryId, ComplaintStatus? status, ComplaintPriority? priority, string? officerId)
    {
        ReportTitle = string.IsNullOrWhiteSpace(reportTitle) ? "Complaint Summary Report" : reportTitle;
        FromDate = fromDate; ToDate = toDate; DepartmentFilter = departmentId; CategoryFilter = categoryId;
        StatusFilter = status; PriorityFilter = priority; OfficerFilter = officerId;

        var rows = await BuildQuery().OrderByDescending(c => c.SubmittedDate).ToListAsync();
        var reportRows = rows.Select(ToReportRow).ToList();
        var bytes = _reportExportService.ExportToPdf(ReportTitle, reportRows);

        await LogExportAsync("PDF");
        return File(bytes, "application/pdf", $"{SafeFileName(ReportTitle)}.pdf");
    }

    private IQueryable<Complaint> BuildQuery()
    {
        var query = _context.Complaints
            .Include(c => c.Category)
            .Include(c => c.Department)
            .Include(c => c.Assignments).ThenInclude(a => a.Officer)
            .AsQueryable();

        if (FromDate.HasValue) query = query.Where(c => c.SubmittedDate >= FromDate.Value);
        if (ToDate.HasValue) query = query.Where(c => c.SubmittedDate < ToDate.Value.AddDays(1));
        if (DepartmentFilter.HasValue) query = query.Where(c => c.DepartmentId == DepartmentFilter);
        if (CategoryFilter.HasValue) query = query.Where(c => c.CategoryId == CategoryFilter);
        if (StatusFilter.HasValue) query = query.Where(c => c.Status == StatusFilter);
        if (PriorityFilter.HasValue) query = query.Where(c => c.Priority == PriorityFilter);
        if (!string.IsNullOrEmpty(OfficerFilter))
        {
            query = query.Where(c => c.Assignments.Any(a => a.OfficerId == OfficerFilter && a.UnassignedDate == null));
        }

        return query;
    }

    private static ComplaintReportRow ToReportRow(Complaint c) => new()
    {
        ReferenceNumber = c.ReferenceNumber,
        Title = c.Title,
        Category = c.Category?.Name ?? "",
        Department = c.Department?.Name ?? "",
        Priority = c.Priority.ToString(),
        Status = c.Status.ToString(),
        AssignedOfficer = c.CurrentOfficer?.FullName ?? "Unassigned",
        SubmittedDate = c.SubmittedDate,
        ResolvedDate = c.ResolvedDate
    };

    private async Task LoadDropdownsAsync()
    {
        Categories = await _context.ComplaintCategories.OrderBy(c => c.Name).ToListAsync();
        Departments = await _context.Departments.OrderBy(d => d.Name).ToListAsync();
        Officers = await _context.Users
            .Where(u => _context.UserRoles.Any(ur => ur.UserId == u.Id &&
                _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == RoleNames.ComplaintOfficer)))
            .OrderBy(u => u.FullName)
            .ToListAsync();
    }

    private async Task LogExportAsync(string format)
    {
        var admin = await _userManager.GetUserAsync(User);
        await _auditLogService.LogAsync(admin?.Id, admin?.FullName ?? "Admin", "Report Exported", $"Exported \"{ReportTitle}\" as {format}.");
    }

    private static string SafeFileName(string title) =>
        string.Join("_", title.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
}
