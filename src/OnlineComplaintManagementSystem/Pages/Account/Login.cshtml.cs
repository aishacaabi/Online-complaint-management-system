using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OnlineComplaintManagementSystem.Models;
using OnlineComplaintManagementSystem.Services;

namespace OnlineComplaintManagementSystem.Pages.Account;

public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditLogService _auditLogService;

    public LoginModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, IAuditLogService auditLogService)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _auditLogService = auditLogService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [TempData]
    public string? ErrorMessage { get; set; }

    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }

    public async Task OnGetAsync(string? returnUrl = null)
    {
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.FindByEmailAsync(Input.Email);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return Page();
        }

        if (!user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Your account has been deactivated. Please contact the system administrator.");
            return Page();
        }

        var result = await _signInManager.PasswordSignInAsync(user.UserName!, Input.Password, Input.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            await _auditLogService.LogAsync(user.Id, user.FullName, "User Login", "User logged in successfully.", HttpContext.Connection.RemoteIpAddress?.ToString());
            return await LocalRedirectToRoleAsync(user);
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(string.Empty, "This account has been locked out due to multiple failed attempts. Please try again later.");
            return Page();
        }

        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return Page();
    }

    private async Task<IActionResult> LocalRedirectToRoleAsync(ApplicationUser user)
    {
        if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
        {
            return LocalRedirect(ReturnUrl);
        }

        if (await _userManager.IsInRoleAsync(user, RoleNames.SuperAdministrator))
        {
            return RedirectToPage("/Admin/Dashboard");
        }
        if (await _userManager.IsInRoleAsync(user, RoleNames.ComplaintOfficer))
        {
            return RedirectToPage("/Officer/Dashboard");
        }
        return RedirectToPage("/Complainant/Dashboard");
    }
}
