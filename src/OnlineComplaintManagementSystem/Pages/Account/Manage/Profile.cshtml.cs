using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OnlineComplaintManagementSystem.Data;
using OnlineComplaintManagementSystem.Models;
using OnlineComplaintManagementSystem.Services;

namespace OnlineComplaintManagementSystem.Pages.Account.Manage;

public class ProfileModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly IFileUploadService _fileUploadService;

    public ProfileModel(UserManager<ApplicationUser> userManager, ApplicationDbContext context, IAuditLogService auditLogService, IFileUploadService fileUploadService)
    {
        _userManager = userManager;
        _context = context;
        _auditLogService = auditLogService;
        _fileUploadService = fileUploadService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string Email { get; set; } = string.Empty;
    public IList<string> Roles { get; set; } = new List<string>();
    public string? DepartmentName { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? ProfilePhotoPath { get; set; }

    public class InputModel
    {
        [Required, StringLength(150)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Phone]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [StringLength(250)]
        public string? Address { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return NotFound();

        await LoadAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return NotFound();

        if (!ModelState.IsValid)
        {
            await LoadAsync(user);
            return Page();
        }

        user.FullName = Input.FullName;
        user.PhoneNumber = Input.PhoneNumber;
        user.Address = Input.Address;

        await _userManager.UpdateAsync(user);
        await _auditLogService.LogAsync(user.Id, user.FullName, "Profile Updated", "User updated their profile information.");

        TempData["SuccessMessage"] = "Your profile has been updated successfully.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUploadPhotoAsync(IFormFile photoFile)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return NotFound();

        var result = await _fileUploadService.SaveProfilePhotoAsync(photoFile, user.Id);
        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.ErrorMessage;
            return RedirectToPage();
        }

        var previousPhoto = user.ProfilePhotoPath;
        user.ProfilePhotoPath = result.StoredPath;
        await _userManager.UpdateAsync(user);

        if (!string.IsNullOrEmpty(previousPhoto))
        {
            _fileUploadService.DeleteProfilePhoto(previousPhoto);
        }

        await _auditLogService.LogAsync(user.Id, user.FullName, "Profile Photo Updated", "User uploaded a new profile photo.");

        TempData["SuccessMessage"] = "Your profile photo has been updated.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRemovePhotoAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return NotFound();

        if (!string.IsNullOrEmpty(user.ProfilePhotoPath))
        {
            _fileUploadService.DeleteProfilePhoto(user.ProfilePhotoPath);
            user.ProfilePhotoPath = null;
            await _userManager.UpdateAsync(user);
            await _auditLogService.LogAsync(user.Id, user.FullName, "Profile Photo Removed", "User removed their profile photo.");
        }

        TempData["SuccessMessage"] = "Your profile photo has been removed.";
        return RedirectToPage();
    }

    private async Task LoadAsync(ApplicationUser user)
    {
        Email = user.Email ?? string.Empty;
        CreatedDate = user.CreatedDate;
        ProfilePhotoPath = user.ProfilePhotoPath;
        Roles = await _userManager.GetRolesAsync(user);

        if (user.DepartmentId.HasValue)
        {
            DepartmentName = await _context.Departments
                .Where(d => d.Id == user.DepartmentId)
                .Select(d => d.Name)
                .FirstOrDefaultAsync();
        }

        Input = new InputModel
        {
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            Address = user.Address
        };
    }
}
