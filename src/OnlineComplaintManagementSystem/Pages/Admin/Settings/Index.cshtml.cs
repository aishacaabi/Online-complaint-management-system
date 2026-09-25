using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OnlineComplaintManagementSystem.Data;
using OnlineComplaintManagementSystem.Models;
using OnlineComplaintManagementSystem.Services;

namespace OnlineComplaintManagementSystem.Pages.Admin.Settings;

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

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public int TotalUsers { get; set; }
    public int TotalAdmins { get; set; }
    public int TotalOfficers { get; set; }
    public int TotalComplainants { get; set; }
    public int TotalDepartments { get; set; }
    public int TotalCategories { get; set; }
    public int TotalComplaints { get; set; }

    public class InputModel
    {
        [Required, StringLength(150)]
        [Display(Name = "Organization Name")]
        public string OrganizationName { get; set; } = string.Empty;

        [Range(1, 90)]
        [Display(Name = "Default Resolution Target (days)")]
        public int DefaultResolutionDays { get; set; } = 14;

        [EmailAddress, StringLength(100)]
        [Display(Name = "Support Email")]
        public string? SupportEmail { get; set; }
    }

    public async Task OnGetAsync()
    {
        await LoadStatsAsync();

        var settings = await _context.SystemSettings.FirstOrDefaultAsync();
        if (settings is not null)
        {
            Input = new InputModel
            {
                OrganizationName = settings.OrganizationName,
                DefaultResolutionDays = settings.DefaultResolutionDays,
                SupportEmail = settings.SupportEmail
            };
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadStatsAsync();
            return Page();
        }

        var settings = await _context.SystemSettings.FirstOrDefaultAsync();
        if (settings is null)
        {
            settings = new SystemSetting();
            _context.SystemSettings.Add(settings);
        }

        settings.OrganizationName = Input.OrganizationName;
        settings.DefaultResolutionDays = Input.DefaultResolutionDays;
        settings.SupportEmail = Input.SupportEmail;
        settings.UpdatedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var admin = await _userManager.GetUserAsync(User);
        await _auditLogService.LogAsync(admin?.Id, admin?.FullName ?? "Admin", "System Settings Updated", "Administrator updated system settings.");

        TempData["SuccessMessage"] = "System settings updated successfully.";
        return RedirectToPage();
    }

    private async Task LoadStatsAsync()
    {
        TotalUsers = await _context.Users.CountAsync();
        TotalAdmins = (await _userManager.GetUsersInRoleAsync(RoleNames.SuperAdministrator)).Count;
        TotalOfficers = (await _userManager.GetUsersInRoleAsync(RoleNames.ComplaintOfficer)).Count;
        TotalComplainants = (await _userManager.GetUsersInRoleAsync(RoleNames.Complainant)).Count;
        TotalDepartments = await _context.Departments.CountAsync();
        TotalCategories = await _context.ComplaintCategories.CountAsync();
        TotalComplaints = await _context.Complaints.CountAsync();
    }
}
