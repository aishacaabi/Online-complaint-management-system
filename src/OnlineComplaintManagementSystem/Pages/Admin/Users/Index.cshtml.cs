using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OnlineComplaintManagementSystem.Data;
using OnlineComplaintManagementSystem.Models;
using OnlineComplaintManagementSystem.Services;

namespace OnlineComplaintManagementSystem.Pages.Admin.Users;

public class UserRow
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; }
    public string Role { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
    public int? DepartmentId { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? ProfilePhotoPath { get; set; }
}

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

    public List<UserRow> Users { get; set; } = new();
    public List<Department> Departments { get; set; } = new();
    public string? SearchTerm { get; set; }
    public string? RoleFilter { get; set; }

    [BindProperty]
    public CreateInput NewUser { get; set; } = new();

    [BindProperty]
    public EditInput EditUser { get; set; } = new();

    [BindProperty]
    public ResetPasswordInput ResetPassword { get; set; } = new();

    public class CreateInput
    {
        [Required, StringLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Phone]
        public string? PhoneNumber { get; set; }

        [Required]
        public string Role { get; set; } = RoleNames.Complainant;

        public int? DepartmentId { get; set; }

        [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 8)]
        public string Password { get; set; } = string.Empty;
    }

    public class EditInput
    {
        [Required]
        public string Id { get; set; } = string.Empty;

        [Required, StringLength(150)]
        public string FullName { get; set; } = string.Empty;

        [Phone]
        public string? PhoneNumber { get; set; }

        [StringLength(250)]
        public string? Address { get; set; }

        [Required]
        public string Role { get; set; } = RoleNames.Complainant;

        public int? DepartmentId { get; set; }
    }

    public class ResetPasswordInput
    {
        [Required]
        public string Id { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 8)]
        public string NewPassword { get; set; } = string.Empty;
    }

    public async Task OnGetAsync(string? search, string? role)
    {
        SearchTerm = search;
        RoleFilter = role;
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        ModelState.Clear();
        TryValidateModel(NewUser, nameof(NewUser));

        if (!ModelState.IsValid)
        {
            await LoadAsync();
            TempData["ErrorMessage"] = "Please correct the errors and try again.";
            return Page();
        }

        var existing = await _userManager.FindByEmailAsync(NewUser.Email);
        if (existing is not null)
        {
            await LoadAsync();
            TempData["ErrorMessage"] = "A user with this email already exists.";
            return Page();
        }

        var user = new ApplicationUser
        {
            UserName = NewUser.Email,
            Email = NewUser.Email,
            FullName = NewUser.FullName,
            PhoneNumber = NewUser.PhoneNumber,
            EmailConfirmed = true,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            DepartmentId = NewUser.Role == RoleNames.ComplaintOfficer ? NewUser.DepartmentId : null
        };

        if (user.DepartmentId.HasValue &&
            !await _context.Departments.AnyAsync(d => d.Id == user.DepartmentId && d.IsActive))
        {
            await LoadAsync();
            TempData["ErrorMessage"] = "Please select an active department.";
            return Page();
        }

        var result = await _userManager.CreateAsync(user, NewUser.Password);
        if (!result.Succeeded)
        {
            await LoadAsync();
            TempData["ErrorMessage"] = string.Join(" ", result.Errors.Select(e => e.Description));
            return Page();
        }

        await _userManager.AddToRoleAsync(user, NewUser.Role);

        var admin = await _userManager.GetUserAsync(User);
        await _auditLogService.LogAsync(admin?.Id, admin?.FullName ?? "Admin", "User Created",
            $"Created user \"{user.Email}\" with role {NewUser.Role}.");

        TempData["SuccessMessage"] = "User created successfully.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEditAsync()
    {
        ModelState.Clear();
        TryValidateModel(EditUser, nameof(EditUser));

        if (!ModelState.IsValid)
        {
            await LoadAsync();
            TempData["ErrorMessage"] = "Please correct the errors and try again.";
            return Page();
        }

        var user = await _userManager.FindByIdAsync(EditUser.Id);
        if (user is null) return NotFound();

        if (EditUser.Role == RoleNames.ComplaintOfficer && EditUser.DepartmentId.HasValue &&
            EditUser.DepartmentId != user.DepartmentId &&
            !await _context.Departments.AnyAsync(d => d.Id == EditUser.DepartmentId && d.IsActive))
        {
            TempData["ErrorMessage"] = "Officers can only be moved to an active department.";
            return RedirectToPage();
        }

        user.FullName = EditUser.FullName;
        user.PhoneNumber = EditUser.PhoneNumber;
        user.Address = EditUser.Address;
        user.DepartmentId = EditUser.Role == RoleNames.ComplaintOfficer ? EditUser.DepartmentId : null;
        await _userManager.UpdateAsync(user);

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (!currentRoles.Contains(EditUser.Role))
        {
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, EditUser.Role);
        }

        var admin = await _userManager.GetUserAsync(User);
        await _auditLogService.LogAsync(admin?.Id, admin?.FullName ?? "Admin", "User Updated", $"Updated user \"{user.Email}\".");

        TempData["SuccessMessage"] = "User updated successfully.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        var admin = await _userManager.GetUserAsync(User);

        // Deleting your own account would immediately lock you out of the system.
        if (admin is not null && admin.Id == user.Id)
        {
            TempData["ErrorMessage"] = "You cannot delete the account you are currently signed in with.";
            return RedirectToPage();
        }

        // Complaints, responses, assignments, status history, and feedback all reference users with
        // restrict-on-delete foreign keys, so check first and explain instead of throwing a SQL error.
        var complaintCount = await _context.Complaints.CountAsync(c => c.ComplainantId == user.Id);
        var assignmentCount = await _context.ComplaintAssignments.CountAsync(a => a.OfficerId == user.Id || a.AssignedById == user.Id);
        var responseCount = await _context.ComplaintResponses.CountAsync(r => r.AuthorId == user.Id);
        var historyCount = await _context.ComplaintStatusHistories.CountAsync(h => h.ChangedById == user.Id);
        var feedbackCount = await _context.Feedbacks.CountAsync(f => f.SubmittedById == user.Id);
        var attachmentCount = await _context.ComplaintAttachments.CountAsync(a => a.UploadedById == user.Id);

        var linked = complaintCount + assignmentCount + responseCount + historyCount + feedbackCount + attachmentCount;
        if (linked > 0)
        {
            TempData["ErrorMessage"] =
                $"\"{user.Email}\" cannot be deleted because the account has {linked} linked complaint record(s) " +
                "(complaints, assignments, messages, status changes, attachments, or feedback). Deleting it would break that history.";
            return RedirectToPage();
        }

        var email = user.Email;
        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = string.Join(" ", result.Errors.Select(e => e.Description));
            return RedirectToPage();
        }

        await _auditLogService.LogAsync(admin?.Id, admin?.FullName ?? "Admin",
            "User Deleted", $"User \"{email}\" permanently deleted.");

        TempData["SuccessMessage"] = $"User \"{email}\" was permanently deleted.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleActiveAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        var admin = await _userManager.GetUserAsync(User);

        // Deactivating your own account would immediately lock you out of the system.
        if (admin is not null && admin.Id == user.Id)
        {
            TempData["ErrorMessage"] = "You cannot deactivate the account you are currently signed in with.";
            return RedirectToPage();
        }

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);

        if (!user.IsActive)
        {
            // Changing the security stamp signs the user out of any session that is already open.
            await _userManager.UpdateSecurityStampAsync(user);
        }

        var action = user.IsActive ? "Activated" : "Deactivated";
        await _auditLogService.LogAsync(admin?.Id, admin?.FullName ?? "Admin",
            $"User {action}", $"User \"{user.Email}\" {action.ToLower()}.");

        if (user.IsActive)
        {
            TempData["SuccessMessage"] = $"\"{user.Email}\" was activated and can sign in again.";
        }
        else
        {
            var openComplaints = await _context.ComplaintAssignments.CountAsync(a =>
                a.OfficerId == user.Id && a.UnassignedDate == null &&
                a.Complaint!.Status != ComplaintStatus.Resolved &&
                a.Complaint.Status != ComplaintStatus.Closed &&
                a.Complaint.Status != ComplaintStatus.Rejected);

            TempData["SuccessMessage"] = openComplaints > 0
                ? $"\"{user.Email}\" was deactivated and can no longer sign in. They still have {openComplaints} open complaint(s) assigned; reassign them to another officer."
                : $"\"{user.Email}\" was deactivated and can no longer sign in. All their records are kept.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostResetPasswordAsync()
    {
        ModelState.Clear();
        TryValidateModel(ResetPassword, nameof(ResetPassword));

        if (!ModelState.IsValid)
        {
            await LoadAsync();
            TempData["ErrorMessage"] = "Please provide a valid password (min 8 characters).";
            return Page();
        }

        var user = await _userManager.FindByIdAsync(ResetPassword.Id);
        if (user is null) return NotFound();

        var removeResult = await _userManager.RemovePasswordAsync(user);
        if (!removeResult.Succeeded)
        {
            TempData["ErrorMessage"] = "Unable to reset password.";
            return RedirectToPage();
        }

        var addResult = await _userManager.AddPasswordAsync(user, ResetPassword.NewPassword);
        if (!addResult.Succeeded)
        {
            TempData["ErrorMessage"] = string.Join(" ", addResult.Errors.Select(e => e.Description));
            return RedirectToPage();
        }

        var admin = await _userManager.GetUserAsync(User);
        await _auditLogService.LogAsync(admin?.Id, admin?.FullName ?? "Admin", "Password Reset by Admin", $"Administrator reset password for \"{user.Email}\".");

        TempData["SuccessMessage"] = "Password reset successfully.";
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        // Inactive departments are loaded too so officers already assigned to one keep showing it;
        // the view only offers active departments for new assignments.
        Departments = await _context.Departments.OrderBy(d => d.Name).ToListAsync();

        var users = await _context.Users
            .OrderBy(u => u.FullName)
            .ToListAsync();

        var rows = new List<UserRow>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            var role = roles.FirstOrDefault() ?? "";

            if (!string.IsNullOrEmpty(RoleFilter) && role != RoleFilter) continue;
            if (!string.IsNullOrWhiteSpace(SearchTerm) &&
                !(u.FullName.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                  (u.Email ?? "").Contains(SearchTerm, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            rows.Add(new UserRow
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email ?? "",
                PhoneNumber = u.PhoneNumber,
                Address = u.Address,
                IsActive = u.IsActive,
                Role = role,
                DepartmentId = u.DepartmentId,
                DepartmentName = Departments.FirstOrDefault(d => d.Id == u.DepartmentId)?.Name,
                CreatedDate = u.CreatedDate,
                ProfilePhotoPath = u.ProfilePhotoPath
            });
        }

        Users = rows;
    }
}
