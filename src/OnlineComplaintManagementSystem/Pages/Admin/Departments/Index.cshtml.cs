using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OnlineComplaintManagementSystem.Data;
using OnlineComplaintManagementSystem.Models;
using OnlineComplaintManagementSystem.Services;

namespace OnlineComplaintManagementSystem.Pages.Admin.Departments;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditLogService _auditLogService;

    public IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IAuditLogService auditLogService)
    {
        _context = context;
        _userManager = userManager;
        _auditLogService = auditLogService;
    }

    public List<Department> Departments { get; set; } = new();
    public Dictionary<int, int> ComplaintCounts { get; set; } = new();
    public Dictionary<int, int> OfficerCounts { get; set; } = new();

    [BindProperty]
    public DepartmentInput Input { get; set; } = new();

    public class DepartmentInput
    {
        public int Id { get; set; }

        [Required, StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [EmailAddress, StringLength(100)]
        public string? ContactEmail { get; set; }

        [StringLength(30)]
        public string? ContactPhone { get; set; }
    }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadAsync();
            TempData["ErrorMessage"] = "Please correct the errors and try again.";
            return Page();
        }

        // Department names are unique in the database; report a clash instead of failing with a SQL error.
        var name = Input.Name.Trim();
        if (await _context.Departments.AnyAsync(d => d.Name == name && d.Id != Input.Id))
        {
            TempData["ErrorMessage"] = $"A department named \"{name}\" already exists.";
            return RedirectToPage();
        }
        Input.Name = name;

        var admin = await _userManager.GetUserAsync(User);

        if (Input.Id == 0)
        {
            var dept = new Department
            {
                Name = Input.Name,
                Description = Input.Description,
                ContactEmail = Input.ContactEmail,
                ContactPhone = Input.ContactPhone,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };
            _context.Departments.Add(dept);
            await _context.SaveChangesAsync();
            await _auditLogService.LogAsync(admin?.Id, admin?.FullName ?? "Admin", "Department Created", $"Department \"{dept.Name}\" created.");
            TempData["SuccessMessage"] = "Department created successfully.";
        }
        else
        {
            var dept = await _context.Departments.FindAsync(Input.Id);
            if (dept is null) return NotFound();

            dept.Name = Input.Name;
            dept.Description = Input.Description;
            dept.ContactEmail = Input.ContactEmail;
            dept.ContactPhone = Input.ContactPhone;
            dept.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _auditLogService.LogAsync(admin?.Id, admin?.FullName ?? "Admin", "Department Updated", $"Department \"{dept.Name}\" updated.");
            TempData["SuccessMessage"] = "Department updated successfully.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var dept = await _context.Departments.FindAsync(id);
        if (dept is null) return NotFound();

        // A department is referenced by complaints and by officer accounts. Deleting it would break that
        // history, so only departments with no related records can be removed; the rest can be deactivated.
        var complaintCount = await _context.Complaints.CountAsync(c => c.DepartmentId == id);
        var officerCount = await _context.Users.CountAsync(u => u.DepartmentId == id);
        if (complaintCount > 0 || officerCount > 0)
        {
            TempData["ErrorMessage"] =
                $"\"{dept.Name}\" cannot be permanently deleted because it has {complaintCount} complaint(s) and {officerCount} user(s) linked to it. " +
                "Deactivate it instead to hide it from new complaints while keeping all existing records.";
            return RedirectToPage();
        }

        var name = dept.Name;
        _context.Departments.Remove(dept);
        await _context.SaveChangesAsync();

        var admin = await _userManager.GetUserAsync(User);
        await _auditLogService.LogAsync(admin?.Id, admin?.FullName ?? "Admin",
            "Department Deleted", $"Department \"{name}\" permanently deleted.");

        TempData["SuccessMessage"] = $"Department \"{name}\" was permanently deleted.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleActiveAsync(int id)
    {
        var dept = await _context.Departments.FindAsync(id);
        if (dept is null) return NotFound();

        dept.IsActive = !dept.IsActive;
        dept.UpdatedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var action = dept.IsActive ? "Activated" : "Deactivated";
        var admin = await _userManager.GetUserAsync(User);
        await _auditLogService.LogAsync(admin?.Id, admin?.FullName ?? "Admin",
            $"Department {action}", $"Department \"{dept.Name}\" {action.ToLower()}.");

        TempData["SuccessMessage"] = dept.IsActive
            ? $"Department \"{dept.Name}\" was reactivated."
            : $"Department \"{dept.Name}\" was deactivated. Its existing complaints and records are kept, but it can no longer be selected for new complaints.";
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        Departments = await _context.Departments.OrderBy(d => d.Name).ToListAsync();
        ComplaintCounts = await _context.Complaints
            .GroupBy(c => c.DepartmentId)
            .Select(g => new { DepartmentId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.DepartmentId, x => x.Count);
        OfficerCounts = await _context.Users
            .Where(u => u.DepartmentId != null)
            .GroupBy(u => u.DepartmentId!.Value)
            .Select(g => new { DepartmentId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.DepartmentId, x => x.Count);
    }
}
