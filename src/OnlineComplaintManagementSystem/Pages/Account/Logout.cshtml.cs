using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OnlineComplaintManagementSystem.Models;
using OnlineComplaintManagementSystem.Services;

namespace OnlineComplaintManagementSystem.Pages.Account;

public class LogoutModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditLogService _auditLogService;

    public LogoutModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, IAuditLogService auditLogService)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _auditLogService = auditLogService;
    }

    public IActionResult OnGet() => RedirectToPage("/Index");

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        await _signInManager.SignOutAsync();

        if (user is not null)
        {
            await _auditLogService.LogAsync(user.Id, user.FullName, "User Logout", "User logged out.", HttpContext.Connection.RemoteIpAddress?.ToString());
        }

        return RedirectToPage("/Index");
    }
}
